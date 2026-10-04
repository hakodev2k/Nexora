import { test, expect, api, confirm, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

test('Career: manual Companies/Jobs, exact SQL salary, merge history, conflicts and paged lifecycle', async ({ page, browser }) => {
  test.setTimeout(240000); page.setDefaultTimeout(15000);
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const superPage = await context.newPage(); await login(superPage, 'SuperAdmin');
  const module = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX39');
  const system = { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled };
  const policy = async (change: typeof system) => {
    const current = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX39');
    if (current.systemEnabled === change.systemEnabled && current.registrationEnabled === change.registrationEnabled) return;
    const preview = await api(superPage, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  await page.goto('/'); const me = await api(page, 'getMe');
  const original = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX39');
  const grant = async (enabled: boolean) => {
    const current = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX39');
    if (current.enabled === enabled) return;
    const changes = [{ moduleId: original.moduleId, enabled }];
    const preview = await api(superPage, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  let failed = false;
  try {
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX39', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Career', exact: true })).toBeVisible();
    const root = '/api/v1/career'; const modal=page.locator('.resource-form-dialog');
    const companyRegion=page.getByRole('region',{name:'Companies',exact:true}); const jobRegion=page.getByRole('region',{name:'Jobs',exact:true});
    const prefix=unique('Career'); const sourceTitle=prefix+' Source'; const targetTitle=prefix+' Target'; const title=prefix+' Job';
    const companyCard=(name:string)=>companyRegion.locator('.resource-card').filter({has:page.getByRole('heading',{name,exact:true})});
    const jobCard=(name:string)=>jobRegion.locator('.resource-card').filter({has:page.getByRole('heading',{name,exact:true})});
    const searchCompanies=async(query:string)=>{await companyRegion.getByLabel('Company search',{exact:true}).fill(query);await companyRegion.getByRole('button',{name:'Search Companies',exact:true}).click();await expect(page.getByRole('button',{name:'Tải lại',exact:true})).toBeEnabled();};
    const searchJobs=async(query:string,state='Active')=>{await jobRegion.getByLabel('Job search',{exact:true}).fill(query);await jobRegion.getByLabel('Job state filter',{exact:true}).selectOption(state);await jobRegion.getByRole('button',{name:'Filter Jobs',exact:true}).click();await expect(page.getByRole('button',{name:'Tải lại',exact:true})).toBeEnabled();};
    await companyRegion.getByRole('button',{name:'New Company',exact:true}).click();await modal.getByLabel('Company title',{exact:true}).fill('Unsaved draft');await modal.getByRole('button',{name:'Hủy',exact:true}).click();await modal.getByRole('button',{name:'Tiếp tục sửa',exact:true}).click();await expect(modal.getByLabel('Company title',{exact:true})).toHaveValue('Unsaved draft');await modal.getByRole('button',{name:'Hủy',exact:true}).click();await modal.getByRole('button',{name:'Bỏ bản nháp',exact:true}).click();
    const companyIds:string[]=[];
    for(const name of [sourceTitle,targetTitle]){
      await companyRegion.getByRole('button',{name:'New Company',exact:true}).click();await modal.getByLabel('Company title',{exact:true}).fill(name);await modal.getByLabel('Company notes',{exact:true}).fill('Private '+name);
      const creation=page.waitForResponse(r=>r.url().endsWith(root+'/companies')&&r.request().method()==='POST');await modal.getByRole('button',{name:'Save Company',exact:true}).click();await expect(modal).toHaveCount(0);companyIds.push((await (await creation).json()).itemId);
    }
    const [sourceId,targetId]=companyIds;await searchCompanies(prefix);expect(sql('Company',sourceId)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    await jobRegion.getByRole('button',{name:'New Job',exact:true}).click();await expect(modal.getByLabel('Job stage',{exact:true})).toHaveValue('');await modal.getByLabel('Job title',{exact:true}).fill(title);await modal.getByLabel('Job stage',{exact:true}).selectOption('Saved');
    await modal.getByLabel('Find Company',{exact:true}).fill(sourceTitle);await modal.getByRole('button',{name:'Search Company picker',exact:true}).click();await modal.getByLabel('Job Company',{exact:true}).selectOption(sourceId);
    await modal.getByLabel('Salary minimum (exact decimal)',{exact:true}).fill('-9007199254740993.12345678');await modal.getByLabel('Salary maximum (exact decimal)',{exact:true}).fill('99999999999999999999.99999999');await modal.getByLabel('Salary currency',{exact:true}).fill('USD');await modal.getByLabel('Discovered on',{exact:true}).fill('2026-10-01');await modal.getByLabel('Applied on',{exact:true}).fill('2026-10-03');await modal.getByLabel('Job notes',{exact:true}).fill('Initial private note');
    const creation=page.waitForResponse(r=>r.url().endsWith(root+'/jobs')&&r.request().method()==='POST');await modal.getByRole('button',{name:'Save Job',exact:true}).click();await expect(modal).toHaveCount(0);const id=(await (await creation).json()).itemId;
    expect(sql('JobApplication',id)[0].CompanyId.toLowerCase()).toBe(sourceId.toLowerCase());await searchJobs(title);await expect(jobCard(title)).toContainText('-9007199254740993.12345678');
    await jobCard(title).getByRole('button',{name:'Edit Job',exact:true}).click();await modal.getByLabel('Job notes',{exact:true}).fill('Deliberate draft');let item=(await response(page,root+'/jobs/'+id,'GET')).body;
    expect((await response(page,root+'/jobs/'+id,'PUT',{metadata:{...item.metadata,notes:'Concurrent note',salaryText:'Concurrent salary text'}},item.etag)).status).toBe(200);
    await modal.getByRole('button',{name:'Save Job',exact:true}).click();await expect(modal).toContainText('Concurrent salary text');await expect(modal).toContainText('Concurrent note');await modal.getByRole('button',{name:'Reapply draft',exact:true}).click();await modal.getByRole('button',{name:'Save Job',exact:true}).click();await expect(modal).toHaveCount(0);expect(sql('JobApplication',id)[0].Notes).toBe('Deliberate draft');
    for(const stage of ['Accepted','Applied']){
      await jobCard(title).getByRole('button',{name:'Change Job stage',exact:true}).click();await modal.getByLabel('Job stage',{exact:true}).selectOption(stage);await modal.getByLabel('Confirm stage change',{exact:true}).check();if(stage==='Applied'){await expect(modal).toContainText('cần lý do riêng tư');await modal.getByLabel('Private stage reason',{exact:true}).fill('Private return reason');await modal.getByLabel('Confirm return to progress',{exact:true}).check();}await modal.getByRole('button',{name:'Save Job stage',exact:true}).click();await expect(modal).toHaveCount(0);
    }
    await searchCompanies(sourceTitle);await companyCard(sourceTitle).getByRole('button',{name:'Merge Company',exact:true}).click();await modal.getByLabel('Find merge target',{exact:true}).fill(targetTitle);await modal.getByRole('button',{name:'Search merge targets',exact:true}).click();await modal.getByLabel('Merge target Company',{exact:true}).selectOption(targetId);await modal.getByRole('button',{name:'Preview merge',exact:true}).click();await expect(modal).toContainText('1 Jobs');await modal.getByLabel('Keep target metadata unchanged',{exact:true}).check();await modal.getByLabel('Confirm Company merge',{exact:true}).check();await modal.getByRole('button',{name:'Commit Company merge',exact:true}).click();await expect(modal).toHaveCount(0);
    expect(sql('JobApplication',id)[0].CompanyId.toLowerCase()).toBe(targetId.toLowerCase());expect(sql('Company',sourceId)[0].MergedIntoId.toLowerCase()).toBe(targetId.toLowerCase());await expect(companyCard(sourceTitle).getByRole('button',{name:'Edit Company',exact:true})).toBeDisabled();
    await jobCard(title).getByRole('button',{name:'Job history',exact:true}).click();let history=page.getByRole('region',{name:'Job history',exact:true});await expect(history).toContainText(sourceTitle);await expect(history).toContainText(targetTitle);await expect(history).toContainText('Private return reason');await history.getByRole('button',{name:'Close Job history',exact:true}).click();
    item=(await response(page,root+'/jobs/'+id,'GET')).body;for(let n=0;n<27;n++){const changed=await response(page,root+'/jobs/'+id,'PUT',{metadata:{...item.metadata,notes:'Paged note '+n}},item.etag);expect(changed.status, typeof changed.body?.code === 'string' ? changed.body.code : 'Opaque update outcome').toBe(200);item={...item,etag:changed.body.etag};}
    await page.getByRole('button',{name:'Tải lại',exact:true}).click();await expect(page.getByRole('button',{name:'Tải lại',exact:true})).toBeEnabled();await jobCard(title).getByRole('button',{name:'Job history',exact:true}).click();await expect(history.locator('h3')).toHaveCount(25);await history.getByRole('button',{name:'Load more Job history',exact:true}).click();await expect(history).toContainText('Initial private note');await history.getByLabel('History action',{exact:true}).selectOption('career.company.merge');await history.getByRole('button',{name:'Filter history',exact:true}).click();await expect(history.locator('h3')).toHaveCount(1);await history.getByRole('button',{name:'Close Job history',exact:true}).click();
    await searchJobs(title);await jobCard(title).getByRole('button',{name:'Move to Trash',exact:true}).click();await confirm(page,'Move to Trash');await searchJobs(title,'Trash');await jobCard(title).getByRole('button',{name:'Restore',exact:true}).click();await confirm(page,'Restore');expect(sql('JobApplication',id)[0].Stage).toBe('Applied');await searchJobs(title);await jobCard(title).getByRole('button',{name:'Move to Trash',exact:true}).click();await confirm(page,'Move to Trash');await searchJobs(title,'Trash');await jobCard(title).getByRole('button',{name:'Delete permanently',exact:true}).click();await confirm(page,'Delete permanently');expect(sql('JobApplication',id)).toEqual([]);
    const paging=unique('Career-paging');for(let n=0;n<29;n++){const made=await response(page,root+'/companies','POST',{metadata:{title:paging+'-'+String(n).padStart(2,'0')}});expect(made.status).toBe(201);const madeJob=await response(page,root+'/jobs','POST',{metadata:{title:paging+'-'+String(n).padStart(2,'0'),companyId:made.body.itemId,notes:'x'.repeat(20000)},stage:'Saved'});expect(madeJob.status).toBe(201);}
    await searchCompanies(paging);await expect(companyRegion.locator('.resource-card')).toHaveCount(25);await companyRegion.getByRole('button',{name:'Load more Companies',exact:true}).click();await expect(companyRegion.locator('.resource-card')).toHaveCount(29);
    await searchJobs(paging);await expect(jobRegion.locator('.resource-card')).toHaveCount(25);await jobRegion.getByRole('button',{name:'Load more Jobs',exact:true}).click();await expect(jobRegion.locator('.resource-card')).toHaveCount(29);await jobRegion.getByLabel('Job view',{exact:true}).selectOption('Kanban');await expect(jobRegion.locator('.resource-card')).toHaveCount(29);
    await expect.poll(()=>page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);await jobRegion.getByLabel('Job view',{exact:true}).selectOption('Table');await expect.poll(()=>page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
    await jobRegion.getByRole('button',{name:'New Job',exact:true}).click();await modal.getByLabel('Find Company',{exact:true}).fill(paging);await modal.getByRole('button',{name:'Search Company picker',exact:true}).click();await expect(modal.getByLabel('Job Company',{exact:true}).locator('option')).toHaveCount(26);await modal.getByRole('button',{name:'Load more Company choices',exact:true}).click();await expect(modal.getByLabel('Job Company',{exact:true}).locator('option')).toHaveCount(30);await modal.getByRole('button',{name:'Hủy',exact:true}).click();await expect(modal).toHaveCount(0);
    await grant(false);await jobCard(paging+'-00').getByRole('button',{name:'Job history',exact:true}).click();await expect(jobRegion.locator('.resource-card')).toHaveCount(0);await expect(companyRegion.locator('.resource-card')).toHaveCount(0);await expect(page.getByRole('button',{name:'Tải lại',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Tải lại',exact:true}).click();await expect(page.getByRole('button',{name:'Tải lại',exact:true})).toBeEnabled();
  } catch(e){failed=true;throw e;}
  finally{try{await grant(original.enabled);await policy(system);}catch(e){if(!failed)throw e;}finally{await context.close();}}
});

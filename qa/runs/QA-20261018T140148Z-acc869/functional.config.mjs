import {defineConfig} from '../../../web/Nexora.Web/node_modules/@playwright/test/index.mjs';
import path from 'node:path';
export default defineConfig({
 testDir:path.resolve(import.meta.dirname,'../../../web/Nexora.Web/functional'),
 outputDir:path.join(import.meta.dirname,'functional-output'),workers:1,retries:0,timeout:90000,
 expect:{timeout:10000},fullyParallel:false,
 reporter:[['line'],['json',{outputFile:path.join(import.meta.dirname,'functional-results.json')}],['junit',{outputFile:path.join(import.meta.dirname,'functional-junit.xml')}]],
 use:{baseURL:process.env.NEXORA_E2E_BASE_URL,channel:'msedge',ignoreHTTPSErrors:false,screenshot:'off',video:'off',trace:'off'},
 projects:[{name:'desktop',use:{viewport:{width:1366,height:768}}}]
});

import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';

export const server = setupServer(http.get('*/api/v1/files/capabilities', () => HttpResponse.json({ 'files.file.read': true, 'files.file.upload': true, 'files.file.download': true })));

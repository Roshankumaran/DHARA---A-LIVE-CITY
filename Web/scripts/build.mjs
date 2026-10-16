import { mkdir, copyFile, cp, readdir } from 'node:fs/promises';
await mkdir('dist', {recursive:true});
for(const file of ['index.html','style.css','app.js','data.js','release.json']) await copyFile(file,`dist/${file}`);
await cp('public','dist',{recursive:true});
console.log('Built static dashboard:',(await readdir('dist')).join(', '));

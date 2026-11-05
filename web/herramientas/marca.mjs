import { cp, mkdir, readdir, readFile, rm, stat, writeFile } from 'node:fs/promises';
import { dirname, extname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const web = join(dirname(fileURLToPath(import.meta.url)), '..');
const fuente = process.env.MARCA_URL ?? 'https://app.codalea.com.co/marca.json';
const soloApi = process.argv.includes('--solo-api');
const destino = process.argv.slice(2).find((a) => !a.startsWith('--')) ?? join(web, '..', 'publicar');
const publicables = ['404.html', 'index.html', 'privacidad.html', 'terminos.html', 'robots.txt', 'sitemap.xml', 'staticwebapp.config.json', 'css', 'js', 'assets'];
const conMarca = new Set(['.html', '.xml', '.txt', '.js', '.json', '.webmanifest']);

async function obtenerMarca() {
  if (process.env.MARCA_PRODUCTO) {
    return { marca: { producto: process.env.MARCA_PRODUCTO, consola: `${process.env.MARCA_PRODUCTO} Admin` }, origen: 'variable MARCA_PRODUCTO' };
  }

  try {
    const respuesta = await fetch(fuente, { signal: AbortSignal.timeout(10000) });
    if (respuesta.ok) {
      const marca = await respuesta.json();
      if (marca.producto) return { marca, origen: fuente };
    }
  } catch {
  }

  return { marca: JSON.parse(await readFile(join(web, 'marca.json'), 'utf8')), origen: 'web/marca.json (respaldo)' };
}

function aplicar(texto, marca) {
  return texto.replaceAll('{{producto}}', marca.producto).replaceAll('{{consola}}', marca.consola);
}

async function copiar(origen, final, marca) {
  if ((await stat(origen)).isDirectory()) {
    await mkdir(final, { recursive: true });
    for (const nombre of await readdir(origen)) await copiar(join(origen, nombre), join(final, nombre), marca);
    return;
  }

  if (conMarca.has(extname(origen))) {
    await writeFile(final, aplicar(await readFile(origen, 'utf8'), marca));
  } else {
    await cp(origen, final);
  }
}

const { marca, origen } = await obtenerMarca();
await writeFile(join(web, 'api', 'src', 'marca.json'), `${JSON.stringify(marca)}\n`);

if (!soloApi) {
  await rm(destino, { recursive: true, force: true });
  await mkdir(destino, { recursive: true });
  for (const nombre of publicables) await copiar(join(web, nombre), join(destino, nombre), marca);
}

console.log(`Marca: ${marca.producto} / ${marca.consola} (fuente: ${origen})${soloApi ? '' : ` → ${destino}`}`);

const test = require('node:test');
const assert = require('node:assert');
const { validar, armarCorreo, crearLimitador, MAXIMO_POR_VENTANA } = require('../src/solicitud');

const valido = { nombre: 'Ana', empresa: 'Motos Ana', correo: 'ana@ejemplo.co', producto: 'Codalea Comercio', canal: 'WhatsApp', autorizaDatos: true };

test('acepta una solicitud completa', () => {
  assert.deepStrictEqual(validar(valido).errores, []);
});

test('exige la autorización de datos y no la acepta como texto', () => {
  assert.ok(validar({ ...valido, autorizaDatos: false }).errores.includes('autorizaDatos'));
  assert.ok(validar({ ...valido, autorizaDatos: 'true' }).errores.includes('autorizaDatos'));
});

test('rechaza correo, producto y canal inválidos', () => {
  const { errores } = validar({ ...valido, correo: 'ana@', producto: 'Otro', canal: 'Fax' });
  assert.deepStrictEqual(errores.sort(), ['canal', 'correo', 'producto']);
});

test('recorta textos largos y quita caracteres de control', () => {
  const { datos } = validar({ ...valido, nombre: 'A\u0000na', mensaje: 'x'.repeat(5000) });
  assert.strictEqual(datos.nombre, 'Ana');
  assert.strictEqual(datos.mensaje.length, 2000);
});

test('el correo escapa el HTML que escribe el visitante', () => {
  const { datos } = validar({ ...valido, mensaje: '<script>alert(1)</script>' });
  const correo = armarCorreo(datos, new Date('2026-10-26T15:00:00Z'));
  assert.ok(!correo.html.includes('<script>'));
  assert.ok(correo.html.includes('&lt;script&gt;'));
  assert.strictEqual(correo.asunto, 'Solicitud de Codalea Comercio · Motos Ana');
});

test('limita los envíos por conexión', () => {
  let t = 0;
  const permitir = crearLimitador(() => t);
  for (let i = 0; i < MAXIMO_POR_VENTANA; i++) assert.ok(permitir('1.2.3.4'));
  assert.ok(!permitir('1.2.3.4'));
  assert.ok(permitir('5.6.7.8'));
  t = 11 * 60 * 1000;
  assert.ok(permitir('1.2.3.4'));
});

const PRODUCTOS = ['AstraSystems', 'Codalea Comercio', 'Los dos'];
const CANALES = ['Correo', 'WhatsApp', 'Llamada'];
const LIMITES = { nombre: 120, empresa: 160, correo: 160, telefono: 30, tamano: 80, mensaje: 2000 };
const VENTANA_MS = 10 * 60 * 1000;
const MAXIMO_POR_VENTANA = 5;

function texto(valor, maximo) {
  if (typeof valor !== 'string') return '';
  return valor.replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, '').trim().slice(0, maximo);
}

function validar(cuerpo) {
  const datos = {
    nombre: texto(cuerpo.nombre, LIMITES.nombre),
    empresa: texto(cuerpo.empresa, LIMITES.empresa),
    correo: texto(cuerpo.correo, LIMITES.correo).toLowerCase(),
    telefono: texto(cuerpo.telefono, LIMITES.telefono),
    producto: texto(cuerpo.producto, 40),
    canal: texto(cuerpo.canal, 20) || 'Correo',
    tamano: texto(cuerpo.tamano, LIMITES.tamano),
    mensaje: texto(cuerpo.mensaje, LIMITES.mensaje),
    autorizaDatos: cuerpo.autorizaDatos === true,
    novedades: cuerpo.novedades === true,
  };
  const errores = [];
  if (!datos.nombre) errores.push('nombre');
  if (!datos.empresa) errores.push('empresa');
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(datos.correo)) errores.push('correo');
  if (!PRODUCTOS.includes(datos.producto)) errores.push('producto');
  if (!CANALES.includes(datos.canal)) errores.push('canal');
  if (!datos.autorizaDatos) errores.push('autorizaDatos');
  return { datos, errores };
}

function escapar(valor) {
  return String(valor).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

function armarCorreo(d, ahora) {
  const filas = [
    ['Nombre', d.nombre],
    ['Empresa', d.empresa],
    ['Correo', d.correo],
    ['Celular', d.telefono || '—'],
    ['Producto', d.producto],
    ['Responder por', d.canal],
    ['Tamaño', d.tamano || '—'],
    ['Autoriza tratamiento de datos', 'Sí'],
    ['Acepta novedades', d.novedades ? 'Sí' : 'No'],
    ['Recibido', ahora.toLocaleString('es-CO', { timeZone: 'America/Bogota' })],
  ];
  const html =
    '<h2 style="font-family:Arial,sans-serif">Nueva solicitud desde codalea.com.co</h2>' +
    '<table style="font-family:Arial,sans-serif;border-collapse:collapse">' +
    filas.map(([k, v]) => `<tr><td style="padding:6px 12px;color:#555">${escapar(k)}</td><td style="padding:6px 12px"><b>${escapar(v)}</b></td></tr>`).join('') +
    '</table>' +
    (d.mensaje ? `<p style="font-family:Arial,sans-serif;white-space:pre-wrap;margin-top:16px">${escapar(d.mensaje)}</p>` : '');
  const textoPlano = filas.map(([k, v]) => `${k}: ${v}`).join('\n') + (d.mensaje ? `\n\n${d.mensaje}` : '');
  return {
    asunto: `Solicitud de ${d.producto} · ${d.empresa}`.slice(0, 180),
    html,
    texto: textoPlano,
  };
}

function crearLimitador(ahora = () => Date.now()) {
  const registro = new Map();
  return function permitir(clave) {
    const t = ahora();
    const previos = (registro.get(clave) || []).filter(x => t - x < VENTANA_MS);
    if (previos.length >= MAXIMO_POR_VENTANA) {
      registro.set(clave, previos);
      return false;
    }
    previos.push(t);
    registro.set(clave, previos);
    if (registro.size > 5000) registro.clear();
    return true;
  };
}

module.exports = { validar, armarCorreo, crearLimitador, MAXIMO_POR_VENTANA };

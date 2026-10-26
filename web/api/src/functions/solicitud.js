const { app } = require('@azure/functions');
const nodemailer = require('nodemailer');
const { validar, armarCorreo, crearLimitador } = require('../solicitud');

const permitir = crearLimitador();
let transporte;

function obtenerTransporte() {
  if (!transporte) {
    transporte = nodemailer.createTransport({
      host: process.env.SMTP_SERVIDOR || 'smtp.azurecomm.net',
      port: Number(process.env.SMTP_PUERTO || 587),
      secure: false,
      requireTLS: true,
      auth: { user: process.env.SMTP_USUARIO, pass: process.env.SMTP_CLAVE },
    });
  }
  return transporte;
}

function respuesta(status, cuerpo) {
  return { status, jsonBody: cuerpo, headers: { 'Cache-Control': 'no-store' } };
}

app.http('solicitud', {
  methods: ['POST'],
  authLevel: 'anonymous',
  route: 'solicitud',
  handler: async (request, context) => {
    let cuerpo;
    try {
      cuerpo = await request.json();
    } catch {
      return respuesta(400, { ok: false, mensaje: 'No pudimos leer la solicitud.' });
    }

    if (cuerpo && typeof cuerpo.sitio === 'string' && cuerpo.sitio.trim() !== '') {
      return respuesta(200, { ok: true });
    }

    const ip = (request.headers.get('x-forwarded-for') || '').split(',')[0].trim() || 'desconocida';
    if (!permitir(ip)) {
      return respuesta(429, { ok: false, mensaje: 'Recibimos varias solicitudes seguidas desde tu conexión. Intenta de nuevo en unos minutos.' });
    }

    const { datos, errores } = validar(cuerpo || {});
    if (errores.length) {
      return respuesta(400, { ok: false, mensaje: 'Revisa los datos del formulario.', campos: errores });
    }

    if (!process.env.SMTP_USUARIO || !process.env.SMTP_CLAVE) {
      context.error('Falta la configuración SMTP del sitio.');
      return respuesta(503, { ok: false, mensaje: 'El formulario no está disponible en este momento.' });
    }

    const correo = armarCorreo(datos, new Date());
    const destino = process.env.CORREO_DESTINO || 'contacto@codalea.com.co';
    try {
      await obtenerTransporte().sendMail({
        from: { name: 'Sitio de Codalea', address: process.env.CORREO_REMITENTE || 'contacto@codalea.com.co' },
        to: destino,
        replyTo: { name: datos.nombre, address: datos.correo },
        subject: correo.asunto,
        text: correo.texto,
        html: correo.html,
      });
    } catch (error) {
      context.error('No se pudo enviar la solicitud', error && error.message);
      return respuesta(502, { ok: false, mensaje: 'No pudimos enviar tu solicitud.' });
    }

    return respuesta(200, { ok: true });
  },
});

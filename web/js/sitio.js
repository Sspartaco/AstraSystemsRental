(function () {
  'use strict';

  document.documentElement.classList.add('js');

  var cabecera = document.querySelector('[data-cabecera]');
  function marcarCabecera() { if (cabecera) cabecera.classList.toggle('es-desplazada', window.scrollY > 8); }
  marcarCabecera();
  window.addEventListener('scroll', marcarCabecera, { passive: true });

  var botonMenu = document.querySelector('[data-menu-boton]');
  var menu = document.querySelector('[data-menu]');
  function cerrarMenu() {
    if (!menu) return;
    menu.classList.remove('es-abierto');
    botonMenu.setAttribute('aria-expanded', 'false');
  }
  if (botonMenu && menu) {
    botonMenu.addEventListener('click', function () {
      var abierto = menu.classList.toggle('es-abierto');
      botonMenu.setAttribute('aria-expanded', abierto ? 'true' : 'false');
    });
    menu.addEventListener('click', function (e) { if (e.target.closest('a')) cerrarMenu(); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') cerrarMenu(); });
  }

  document.querySelectorAll('[data-segmento]').forEach(function (boton) {
    boton.addEventListener('click', function () {
      var elegido = boton.getAttribute('data-segmento');
      document.querySelectorAll('[data-segmento]').forEach(function (b) {
        b.setAttribute('aria-pressed', b === boton ? 'true' : 'false');
      });
      document.querySelectorAll('[data-segmento-texto]').forEach(function (p) {
        p.hidden = p.getAttribute('data-segmento-texto') !== elegido;
      });
    });
  });

  var visor = document.querySelector('[data-visor]');
  if (visor && typeof visor.showModal === 'function') {
    var imagenVisor = visor.querySelector('[data-visor-imagen]');
    var pieVisor = visor.querySelector('[data-visor-pie]');
    document.querySelectorAll('[data-ampliar]').forEach(function (boton) {
      boton.addEventListener('click', function () {
        var img = boton.querySelector('img');
        imagenVisor.src = img.currentSrc || img.src;
        imagenVisor.alt = img.alt;
        pieVisor.textContent = img.alt;
        visor.showModal();
      });
    });
    visor.querySelector('[data-visor-cerrar]').addEventListener('click', function () { visor.close(); });
    visor.addEventListener('click', function (e) { if (e.target === visor) visor.close(); });
  } else {
    document.querySelectorAll('[data-ampliar]').forEach(function (b) { b.style.cursor = 'default'; });
  }

  var selectorProducto = document.querySelector('[data-producto]');
  document.querySelectorAll('[data-producto-destino]').forEach(function (enlace) {
    enlace.addEventListener('click', function () {
      var producto = enlace.getAttribute('data-producto-destino');
      if (selectorProducto && producto) selectorProducto.value = producto;
    });
  });

  var reducido = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (!reducido && 'IntersectionObserver' in window) {
    var observador = new IntersectionObserver(function (entradas) {
      entradas.forEach(function (entrada) {
        if (entrada.isIntersecting) {
          entrada.target.classList.add('es-visible');
          observador.unobserve(entrada.target);
        }
      });
    }, { rootMargin: '0px 0px -8% 0px' });
    document.querySelectorAll('.producto, .paso, .pilares li, .llamado, .movil, .telefonos figure, .incluye').forEach(function (el) {
      el.classList.add('aparece');
      observador.observe(el);
    });
  }

  var formulario = document.querySelector('[data-formulario]');
  if (!formulario) return;
  var estado = formulario.querySelector('[data-estado]');
  var enviar = formulario.querySelector('[data-enviar]');
  var alternativa = ' Escríbenos por <a href="https://wa.me/573229136824">WhatsApp</a> o a <a href="mailto:contacto@codalea.com.co">contacto@codalea.com.co</a>.';

  function mostrar(texto, tipo, conAlternativa) {
    estado.className = 'formulario__estado' + (tipo ? ' es-' + tipo : '');
    estado.innerHTML = '';
    estado.appendChild(document.createTextNode(texto));
    if (conAlternativa) estado.insertAdjacentHTML('beforeend', alternativa);
  }

  function limpiarErrores() {
    formulario.querySelectorAll('[aria-invalid]').forEach(function (el) { el.removeAttribute('aria-invalid'); el.removeAttribute('aria-describedby'); });
    formulario.querySelectorAll('.campo__error').forEach(function (el) { el.remove(); });
  }

  function marcar(campo, mensaje) {
    campo.setAttribute('aria-invalid', 'true');
    var error = document.createElement('p');
    error.className = 'campo__error';
    error.id = campo.id + '-error';
    error.textContent = mensaje;
    campo.setAttribute('aria-describedby', error.id);
    (campo.closest('.casilla') || campo).insertAdjacentElement('afterend', error);
  }

  formulario.addEventListener('submit', function (e) {
    e.preventDefault();
    if (enviar.disabled) return;
    limpiarErrores();

    var primero = null;
    formulario.querySelectorAll('[required]').forEach(function (campo) {
      var vacio = campo.type === 'checkbox' ? !campo.checked : !campo.value.trim();
      var mensaje = null;
      if (vacio) mensaje = campo.type === 'checkbox' ? 'Necesitamos tu autorización para responderte.' : 'Completa este campo.';
      else if (campo.type === 'email' && !/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(campo.value.trim())) mensaje = 'Revisa el correo: parece incompleto.';
      if (mensaje) {
        if (!campo.id) campo.id = 'f-' + campo.name;
        marcar(campo, mensaje);
        primero = primero || campo;
      }
    });
    if (primero) {
      mostrar('Revisa los campos marcados.', 'error', false);
      primero.focus();
      return;
    }

    if (!navigator.onLine) {
      mostrar('No tienes conexión en este momento.', 'error', true);
      return;
    }

    var datos = {};
    new FormData(formulario).forEach(function (valor, clave) { datos[clave] = typeof valor === 'string' ? valor.trim() : valor; });
    datos.autorizaDatos = !!formulario.querySelector('[name="autorizaDatos"]').checked;
    datos.novedades = !!formulario.querySelector('[name="novedades"]').checked;

    enviar.disabled = true;
    enviar.textContent = 'Enviando…';
    mostrar('', null, false);

    var controlador = 'AbortController' in window ? new AbortController() : null;
    var limite = setTimeout(function () { if (controlador) controlador.abort(); }, 15000);

    fetch(formulario.getAttribute('action'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(datos),
      signal: controlador ? controlador.signal : undefined
    })
      .then(function (r) { return r.json().catch(function () { return {}; }).then(function (j) { return { ok: r.ok && j.ok === true, cuerpo: j }; }); })
      .then(function (res) {
        if (res.ok) {
          formulario.reset();
          mostrar('¡Recibido! Te respondemos en horario hábil por el canal que elegiste.', 'exito', false);
          return;
        }
        mostrar((res.cuerpo && res.cuerpo.mensaje) || 'No pudimos enviar tu solicitud.', 'error', true);
      })
      .catch(function () { mostrar('No pudimos enviar tu solicitud.', 'error', true); })
      .finally(function () {
        clearTimeout(limite);
        enviar.disabled = false;
        enviar.textContent = 'Enviar solicitud';
      });
  });
})();

(function () {
  'use strict';

  var SCREENS = {
    panel: {
      label: 'Panel',
      render: function () {
        return (
          card('<p class="ap-greet">Buenas tardes</p><p class="ap-user">Operación Bogotá</p>') +
          '<div class="ap-kpis">' +
            kpi('12', 'Activos', 'ok') +
            kpi('3', 'En taller', 'warn') +
            kpi('2', 'Vencidos', 'bad') +
            kpi('87%', 'Cobertura', 'ok') +
          '</div>' +
          section('Próximas reservas') +
          row('ABC123', 'Taller Norte &middot; hoy 14:00', 'Pendiente', 'warn') +
          row('DEF456', 'Serviteca Sur &middot; mañana', 'Pendiente', 'warn')
        );
      }
    },
    flota: {
      label: 'Flota',
      render: function () {
        return (
          '<div class="ap-search">Buscar por placa&hellip;</div>' +
          vehicle('ABC123', 'Toyota Corolla 2020', 'Activo', 'ok') +
          vehicle('DEF456', 'Renault Duster 2021', 'Activo', 'ok') +
          vehicle('GHI789', 'Mazda CX-5 2019', 'En taller', 'warn') +
          vehicle('JKL012', 'Nissan Frontier 2022', 'Borrador', 'muted')
        );
      }
    },
    recorrido: {
      label: 'Recorrido',
      render: function () {
        return (
          card(
            '<p class="ap-label">Vehículo</p><p class="ap-plate">ABC123</p>' +
            '<p class="ap-hint">Última lectura: 45.200 km &middot; hace 6 días</p>'
          ) +
          '<div class="ap-field"><span>Nuevo kilometraje</span><b>46.850</b></div>' +
          '<p class="ap-valid">Entre 45.200 y 47.800 &mdash; validado en el celular</p>' +
          '<button class="ap-cta" type="button" disabled>Registrar lectura</button>' +
          section('Historial') +
          row('45.200 km', '12/08/2026', 'Manual', 'muted') +
          row('43.980 km', '28/07/2026', 'Taller', 'muted')
        );
      }
    },
    taller: {
      label: 'Taller',
      render: function () {
        return (
          section('Reservas activas') +
          row('ABC123', 'Taller Norte &middot; 18/08 14:00', 'En taller', 'warn') +
          row('GHI789', 'Serviteca Sur &middot; 19/08 09:00', 'Listo', 'ok') +
          card(
            '<p class="ap-label">Fotos del vehículo</p>' +
            '<div class="ap-photos"><span>Al entregar</span><span>Al retirar</span></div>' +
            '<p class="ap-hint">La cámara asocia cada foto al estado de la reserva.</p>'
          ) +
          '<p class="ap-offline">Sin señal, la reserva se guarda y se envía sola.</p>'
        );
      }
    }
  };

  function card(inner) { return '<div class="ap-card">' + inner + '</div>'; }
  function section(t) { return '<p class="ap-section">' + t + '</p>'; }

  function kpi(value, label, tone) {
    return '<div class="ap-kpi ap-kpi--' + tone + '"><b>' + value + '</b><span>' + label + '</span></div>';
  }

  function row(title, meta, badge, tone) {
    return '<div class="ap-row"><div><p>' + title + '</p><span>' + meta + '</span></div>' +
      '<em class="ap-badge ap-badge--' + tone + '">' + badge + '</em></div>';
  }

  function vehicle(plate, model, badge, tone) {
    return '<div class="ap-row ap-row--veh"><div><p class="ap-plate-sm">' + plate + '</p><span>' + model + '</span></div>' +
      '<em class="ap-badge ap-badge--' + tone + '">' + badge + '</em></div>';
  }

  function init(root) {
    var screen = root.querySelector('[data-ap-screen]');
    var tabs = root.querySelectorAll('[data-ap-tab]');
    if (!screen || !tabs.length) return;

    function show(key) {
      var def = SCREENS[key];
      if (!def) return;
      screen.innerHTML = def.render();
      screen.scrollTop = 0;
      for (var i = 0; i < tabs.length; i++) {
        var active = tabs[i].getAttribute('data-ap-tab') === key;
        tabs[i].classList.toggle('is-active', active);
        tabs[i].setAttribute('aria-selected', active ? 'true' : 'false');
      }
    }

    for (var i = 0; i < tabs.length; i++) {
      tabs[i].addEventListener('click', function () {
        show(this.getAttribute('data-ap-tab'));
      });
    }

    show('panel');
  }

  function boot() {
    var root = document.getElementById('app-preview');
    if (!root) return;

    // Solo se arma cuando el usuario se acerca: en la portada no cuesta nada.
    if (!('IntersectionObserver' in window)) { init(root); return; }

    var io = new IntersectionObserver(function (entries) {
      for (var i = 0; i < entries.length; i++) {
        if (entries[i].isIntersecting) {
          init(root);
          io.disconnect();
          return;
        }
      }
    }, { rootMargin: '300px' });

    io.observe(root);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();

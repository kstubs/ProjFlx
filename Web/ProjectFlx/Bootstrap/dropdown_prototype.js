/* ========================================================================
 * Bootstrap: dropdown.js v3.3.6
 * http://twbs.github.com/bootstrap/javascript.html#dropdowns
  * ========================================================================
 * Copyright 2011-2016 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */

/*

Modified for use with PrototypeJS

https://github.com/jwestbrook/bootstrap-prototype/tree/master-3.0


*/

'use strict';

window.BootStrap = window.BootStrap || { };

 /* DROPDOWN CLASS DEFINITION
 * ========================= */
 BootStrap.Dropdown = Class.create({
  VERSION: '3.4.1',
  initialize : function (element) {
    this.$element = $(element);
    this.$element.store('bootstrap:button',this);

    this.menu = this.$element.next('.dropdown-menu');

    document.observe('click',this.clearMenus.bind(this));
    this.$element.on('click',this.toggle.bind(this));
  },
  getParent: function() {
    var element = this.$element;
    var selector = element.readAttribute('data-target');
    var parent;

    if (!selector) {
      selector = element.readAttribute('href');
        selector = selector && /#/.test(selector) && selector.replace(/.*(?=#[^\s]*$)/, '') && selector != '#'; //strip for ie7
      }


      if(!selector) return element.up();
      var parent = $$(selector)[0];
      if(!parent) return element.up();

      return parent;
    },
    clearMenus : function() {
      // TODO: if mouse right click return
      $$('.dropdown-backdrop').invoke('remove');
      $$('.dropdown-menu').invoke('removeClassName', 'show');

      var parent = this.getParent();
      parent.removeClassName('open');

      document.fire('hidden.bs.dropdown');
    }, 
    toggle: function (e) {
      var element = this.$element;
      var parent;
      var isActive;

      if (element.hasClassName('disabled') || element.readAttribute('disabled') == 'disabled') return;

      var parent = this.getParent();

      isActive = parent.hasClassName('open');

      this.clearMenus();

      if (!isActive) {
        if ('ontouchstart' in document.documentElement) {
        // if mobile we we use a backdrop because click events don't delegate
        var backdrop = new Element('div',{'class':'dropdown-backdrop'});
        backdrop.observe('click',this.clearMenus.bind(this));
        element.insert({'before':backdrop});
      }

      this.menu.addClassName('show');
      parent.addClassName('open');
      element.fire('focus');
      element.writeAttribute('aria-expanded', 'true');
      parent.fire('shown.bs.dropdown', element);
    }

    element.focus();
    e.stop();
  }, 
  keydown: function (e) {
    var element = this.$element;
    var items;
    var active;
    var isActive;
    var index;

    if (!/(38|40|27|32)/.test(e.keyCode) || 
      /input|textarea/i.test(e.target.tagName)) return;
      
    e.preventDefault();
    e.stopPropagation();

    if (element.hasClassName('disabled') || element.readAttribute('disabled') == 'disabled') return;

    var parent = this.getParent();

    isActive = parent.hasClassName('open');

    if (!isActive || (isActive && e.keyCode == Event.KEY_ESC))
    {
      if (e.which == Event.KEY_ESC) {
        parent.select('[data-toggle=dropdown]')[0].focus();
        return element.click();   // TODO: test this
      }
    }

    // :visible is a jQuery extension - NOT VALID CSS
    //      items = parent.select('[role=menu] li:not(.divider):visible a')
    //
    items = parent.select('[role=menu] li:not(.divider) a');

    if (!items.length) return;

    index = -1;
    items.each(function(item,i){
      item.match(':focus') ? index = i : '';
    })

    if (e.keyCode == Event.KEY_UP && index > 0) index--;
    if (e.keyCode == Event.KEY_DOWN && index < items.length - 1) index++;
    if (!~index) index = 0

      items[index].focus();
  }
});

 /*domload*/

/* APPLY TO STANDARD DROPDOWN ELEMENTS
* =================================== */

document.observe('dom:loaded',function(){
  $$('[data-toggle=dropdown]').each(function(elm) {
    new BootStrap.Dropdown(elm);
  });

/*document.observe('dom:loaded',function(){
  document.observe('click',BootStrap.Dropdown.prototype.clearMenus)
  $$('.dropdown form').invoke('observe','click',function(e){
    e.stop();
  });
  $$('[data-toggle=dropdown]').invoke('observe','click',BootStrap.Dropdown.prototype.toggle)
  $$('[data-toggle=dropdown]'+', [role=menu]').invoke('observe','keydown',BootStrap.Dropdown.prototype.keydown)
  */
});
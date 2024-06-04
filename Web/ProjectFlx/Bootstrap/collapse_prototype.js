/* ========================================================================
 * Bootstrap: collapse.js v3.0.0
 * http://twbs.github.com/bootstrap/javascript.html#collapse
 * ========================================================================
 * Copyright 2012 Twitter, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ======================================================================== */
/*

Modified for use with PrototypeJS

https://github.com/jwestbrook/bootstrap-prototype/tree/master-3.0


*/

'use strict';

if(BootStrap === undefined)
{
	var BootStrap = {};
}

 /* COLLAPSE PUBLIC CLASS DEFINITION
 * ================================ */

 BootStrap.Collapse = Class.create({
 	initialize : function (element, options) {
 		this.$element = $(element);

 		this.$element.store('bootstrap:collapse',this);

 		this.options = {
 			toggle: true
 		}

 		Object.extend(this.options,options);

 		if (this.options.parent)
 		{
 			this.$parent = $(this.options.parent);
 		}

 		var dimension = this.dimension();
 		this.dim_value = this.$element['get'+dimension.capitalize()]()
 		this.dim_object = {};
 		this.dim_object[dimension] = this.dim_value+'px';
 		this.$element.setStyle(this.dim_object);
 		this.clean_style = {};
 		this.clean_style[dimension] = '';
 		if(this.options.toggle)
 		{
 			this.toggle();
 		}
 	}

 	, dimension: function () {
 		var hasWidth = this.$element.hasClassName('width');
 		return hasWidth ? 'hidth' : 'height';
 	}

 	, show: function () {
 		if (this.transitioning || this.$element.hasClassName('show')) return;

 		var startEvent = this.$element.fire('bootstrap:show');
 		if(startEvent.defaultPrevented) return;

 			var actives = this.$parent && this.$parent.select('.show');

 		if (actives && actives.length) {
 			actives.each(function(el){
 				var bootstrapobject = el.retrieve('bootstrap:collapse')
 				if (bootstrapobject && bootstrapobject.transitioning) return;
 					bootstrapobject.hide();
 			});
 		}

 		var dimension = this.dimension()
 		this.$element.setStyle(this.clean_style);

 		this.transitioning = 1;

 		var complete = function () {
 			this.transitioning = 0
 			this.$element.removeClassName('collapsing').addClassName('show')
 			this.$element.setStyle(this.dim_object)
 			this.$element.fire('bootstrap:shown')
 			this.$element.stopObserving(BootStrap.transitionendevent,complete)
 		}.bind(this)


 		if(BootStrap.handleeffects == 'css')
 		{
 			this.$element.observe(BootStrap.transitionendevent, complete);
 			this.$element.addClassName('collapsing');
			this.$element.setStyle(this.dim_object);

 			complete.delay(.350);
 		}
 		else if(BootStrap.handleeffects == 'effect' && typeof Effect !== 'undefined' && typeof Effect.BlindDown !== 'undefined')
 		{
 			this.$element.blindDown({duration:0.350,beforeStart:function(effect){
 				effect.element.hide()
 				effect.element.addClassName('show')
 			}.bind(this), afterFinish: function(effect){
 				complete()
 			}.bind(this)})
 		}
 		else
 		{
 			complete.delay(.350);
 		}
 	}

 	, hide: function () {
 		if (this.transitioning || !this.$element.hasClassName('show')) return;

 		var startEvent = this.$element.fire('bootstrap:hide');
 		if(startEvent.defaultPrevented) return;

 		var dimension = this.dimension();

 		var complete = function () {
 			this.transitioning = 0;
 			this.$element.fire('bootstrap:hidden');
 			this.$element.removeClassName('collapsing');
 			this.$element.setStyle(this.dim_object);
 			this.$element.stopObserving(BootStrap.transitionendevent,complete);
 		}.bind(this)

 		if(BootStrap.handleeffects == 'css')
 		{
 			this.$element.observe(BootStrap.transitionendevent, complete);
 			this.$element.addClassName('collapsing').removeClassName('show');
 			this.$element.setStyle(this.clean_style);
 		}
 		else if(BootStrap.handleeffects == 'effect' && typeof Effect !== 'undefined' && typeof Effect.BlindUp !== 'undefined')
 		{
 			this.$element.blindUp({duration:0.350,afterFinish:function(effect){
 				effect.element.removeClassName('show');
 				effect.element.show();
 				complete();
 			}.bind(this)})
 		}
 		else
 		{
 			complete();
 		}
 	}

 	, toggle: function () {
 		this[this.$element.hasClassName('show') ? 'hide' : 'show']();
 	}

 });




 /*domload*/

 document.observe('dom:loaded',function(){
 	function findTarget(elm) {
 		var target = elm.readAttribute('data-target') || elm.readAttribute('href');
 		if(target && target.startsWith('#'))
 			target = target.substr(1);

 		target = $(target);

 		return target;
 	}
 	$$('[data-toggle="collapse"]').each(function(elm){
 		var target = findTarget(elm);
 		if(!target) return;

 		var options = {toggle : false}
 		if(elm.hasAttribute('data-parent')){
 			options.parent = elm.readAttribute('data-parent').replace('#','')
 		}
 		if(target.hasClassName('show')){
 			target.addClassName('collapsed');
 		} else {
 			target.removeClassName('collapsed');
 		}
 		new BootStrap.Collapse(target,options);
 	});

 	document.on('click','[data-toggle="collapse"]',function(e){
 		e.stop();
 		var elm = e.findElement('[data-toggle]');
 		var target = findTarget(elm);
 		if(!target) return;
 		target.retrieve('bootstrap:collapse').toggle();
 	});
 });
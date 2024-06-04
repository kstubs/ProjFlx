// Input 0
var BootStrap = {transitionendevent:null, handleeffects:null};
var transEndEventNames = $H({"WebkitTransition":"webkitTransitionEnd", "MozTransition":"transitionend", "OTransition":"oTransitionEnd otransitionend", "transition":"transitionend"});
var el = new Element("bootstrap");
transEndEventNames.each(function(pair) {
  if (el.style[pair.key] !== undefined) {
    BootStrap.transitionendevent = pair.value;
    BootStrap.handleeffects = "css";
  }
});
if (BootStrap.handleeffects === null && typeof Scriptaculous !== "undefined" && typeof Effect !== "undefined") {
  BootStrap.handleeffects = "effect";
}
"use strict";
if (BootStrap === undefined) {
  var BootStrap = {};
}
BootStrap.Affix = Class.create({initialize:function(element, options) {
  this.$element = $(element);
  this.$element.store("bootstrap:affix", this);
  this.options = {offset:0};
  Object.extend(this.options, options);
  Event.observe(window, "scroll", this.checkPosition.bind(this));
  Event.observe(window, "click", this.checkPositionWithEventLoop.bind(this));
  this.affixed = null;
  this.unpin = null;
  this.checkPosition();
}, checkPositionWithEventLoop:function() {
  setTimeout(this.checkPosition.bind(this), 1);
}, checkPosition:function() {
  if (!this.$element.visible()) {
    return;
  }
  var scrollHeight = document.viewport.getHeight();
  var scrollTop = window.pageYOffset || document.documentElement.scrollTop;
  var position = this.$element.positionedOffset();
  var offset = this.options.offset;
  var offsetBottom = offset.bottom;
  var offsetTop = offset.top;
  var reset = "affix affix-top affix-bottom";
  if (typeof offset != "object") {
    offsetBottom = offsetTop = offset;
  }
  if (typeof offsetTop == "function") {
    offsetTop = offset.top();
  }
  if (typeof offsetBottom == "function") {
    offsetBottom = offset.bottom();
  }
  var affix = this.unpin != null && scrollTop + this.unpin <= position.top ? false : offsetBottom != null && position.top + this.$element.getHeight() >= scrollHeight - offsetBottom ? "bottom" : offsetTop != null && scrollTop <= offsetTop ? "top" : false;
  if (this.affixed === affix) {
    return;
  }
  if (this.unpin) {
    this.$element.setStyle({"top":""});
  }
  this.affixed = affix;
  this.unpin = affix == "bottom" ? position.top - scrollTop : null;
  this.$element.removeClassName(reset).addClassName("affix" + (affix ? "-" + affix : ""));
  if (affix == "bottom") {
    this.$element.setStyle({top:document.body.offsetHeight - offsetBottom - this.$element.getHeight()});
  }
}});
document.observe("dom:loaded", function() {
  $$('[data-spy="affix"]').each(function($spy) {
    var data = {};
    data.offset = $spy.hasAttribute("data-offset") ? $spy.readAttribute("data-offset") : {};
    $spy.hasAttribute("data-offset-bottom") ? data.offset.bottom = $spy.readAttribute("data-offset-bottom") : "";
    $spy.hasAttribute("data-offset-top") ? data.offset.top = $spy.readAttribute("data-offset-top") : "";
    new BootStrap.Affix($spy, data);
  });
});
"use strict";
if (BootStrap === undefined) {
  var BootStrap = {};
}
BootStrap.Alert = Class.create({initialize:function(element) {
  element = $(element);
  element.store("bootstrap:alert", this);
  element.on("click", '[data-dismiss="alert"]', this.close);
}, close:function(e) {
  var $this = $(this);
  var selector = $this.readAttribute("data-target");
  if (!selector) {
    selector = $this.href;
    selector = selector && selector.replace(/.*(?=#[^\s]*$)/, "");
  }
  var $parent = $$(selector);
  if (e) {
    e.preventDefault();
    e.stop();
  }
  if (!$parent.length) {
    $parent = $this.hasClassName("alert") ? $this : $this.up();
  }
  var closeEvent = $parent.fire("bootstrap:close");
  if (closeEvent.defaultPrevented) {
    return;
  }
  function removeElement() {
    $parent.fire("bootstrap:closed");
    $parent.remove();
  }
  if (BootStrap.handleeffects === "css" && $parent.hasClassName("fade")) {
    $parent.observe(BootStrap.transitionendevent, removeElement);
    $parent.removeClassName("in");
  } else {
    if (BootStrap.handleeffects === "effect" && $parent.hasClassName("fade")) {
      new Effect.Fade($parent, {duration:0.3, from:$parent.getOpacity() * 1, afterFinish:function() {
        $parent.removeClassName("in");
        removeElement();
      }});
    } else {
      removeElement();
    }
  }
}});
document.observe("dom:loaded", function() {
  document.on("click", '[data-dismiss="alert"]', BootStrap.Alert.prototype.close);
  $$('.alert [data-dismiss="alert"]').each(function(i) {
    new BootStrap.Alert(i);
  });
});
"use strict";
if (BootStrap === undefined) {
  var BootStrap = {};
}
BootStrap.Carousel = Class.create({initialize:function(element, options) {
  this.options = {interval:5000, pause:"hover", wrap:true};
  this.$element = $(element);
  element.store("bootstrap:carousel", this);
  this.$indicators = this.$element.down(".carousel-indicators");
  this.interval = null;
  this.options.interval = this.$element.hasAttribute("data-interval") ? this.$element.readAttribute("data-interval") : this.options.interval;
  Object.extend(this.options, options);
  this.options.slide && this.slide(this.options.slide);
  this.options.pause == "hover" && this.$element.on("mouseenter", this.pause.bind(this)) && this.$element.on("mouseleave", this.cycle.bind(this));
  if (this.options.interval) {
    this.cycle();
  }
}, cycle:function(e) {
  if (!e) {
    this.paused = false;
  }
  this.interval && clearInterval(this.interval);
  this.options.interval && !this.paused && (this.interval = setInterval(this.next.bind(this), this.options.interval));
  return this;
}, getActiveIndex:function() {
  this.$active = this.$element.down(".item.active");
  this.$items = this.$active.up().childElements();
  return this.$items.indexOf(this.$active);
}, to:function(pos) {
  var activeIndex = this.getActiveIndex();
  if (pos > this.$items.length - 1 || pos < 0) {
    return;
  }
  if (this.sliding) {
    return this.$element.on("bootstrap:slid", function() {
      this.to(pos);
    }.bind(this));
  }
  if (activeIndex == pos) {
    return this.pause().cycle();
  }
  return this.slide(pos > activeIndex ? "next" : "previous", $(this.$items[pos]));
}, pause:function(e) {
  if (!e) {
    this.paused = true;
  }
  if (this.$element.select(".next, .prev").length && BootStrap.handleeffects == "css") {
    this.$element.fire(BootStrap.transitionendevent);
    this.cycle(true);
  }
  this.interval = clearInterval(this.interval);
  return this;
}, next:function() {
  if (this.sliding) {
    return;
  }
  return this.slide("next");
}, prev:function() {
  if (this.sliding) {
    return;
  }
  return this.slide("previous");
}, slide:function(type, next) {
  var $active = this.$element.down(".item.active");
  var $next = next || $active[type]();
  var isCycling = this.interval;
  var direction = type == "next" ? "left" : "right";
  var fallback = type == "next" ? "first" : "last";
  var slideEventmemo;
  var slideEvent;
  if ($next === undefined) {
    if (!this.options.wrap) {
      return;
    }
    $next = this.$element.select(".item")[fallback]();
  }
  this.sliding = true;
  isCycling && this.pause();
  slideEventmemo = {relatedTarget:$next, direction:direction};
  type = type == "previous" ? "prev" : type;
  if ($next.hasClassName("active")) {
    return;
  }
  if (this.$indicators) {
    this.$indicators.down(".active").removeClassName("active");
    this.$element.observe("bootstrap:slid", function() {
      var $nextIndicator = $(this.$indicators.childElements()[this.getActiveIndex()]);
      $nextIndicator && $nextIndicator.addClassName("active");
      this.$element.stopObserving("bootstrap:slid");
    }.bind(this));
  }
  if (BootStrap.handleeffects == "css" && this.$element.hasClassName("slide")) {
    slideEvent = this.$element.fire("bootstrap:slide", slideEventmemo);
    if (slideEvent.defaultPrevented) {
      return;
    }
    this.$element.observe(BootStrap.transitionendevent, function(e) {
      $next.removeClassName(type).removeClassName(direction).addClassName("active");
      $active.removeClassName("active").removeClassName(direction);
      this.sliding = false;
      setTimeout(function() {
        this.$element.fire("bootstrap:slid");
      }.bind(this), 0);
      this.$element.stopObserving(BootStrap.transitionendevent);
      isCycling && this.cycle();
    }.bind(this));
    $next.addClassName(type);
    setTimeout(function() {
      $next.addClassName(direction);
      $active.addClassName(direction);
    }, 0);
  } else {
    if (BootStrap.handleeffects == "effect" && typeof Effect !== "undefined" && typeof Effect.Morph !== "undefined") {
      new Effect.Parallel([new Effect.Morph($next, {"sync":true, "style":"left:0%;"}), new Effect.Morph($active, {"sync":true, "style":"left:" + (direction == "left" ? "-" : "") + "100%;"})], {"duration":0.6, "beforeSetup":function(effect) {
        $next.addClassName(type);
        this.sliding = true;
      }.bind(this), "afterFinish":function(effect) {
        $next.removeClassName(type).addClassName("active");
        $active.removeClassName("active");
        $next.style[direction] = null;
        $active.style[direction] = null;
        this.sliding = false;
        this.$element.fire("bootstrap:slid");
        isCycling && this.cycle();
      }.bind(this)});
    } else {
      slideEvent = this.$element.fire("bootstrap:slide", slideEventmemo);
      if (slideEvent.defaultPrevented) {
        return;
      }
      $active.removeClassName("active");
      $next.addClassName("active");
      this.sliding = false;
      this.$element.fire("bootstrap:slid");
      isCycling && this.cycle();
    }
  }
  return this;
}});
document.observe("dom:loaded", function() {
  document.on("click", "[data-slide], [data-slide-to]", function(e) {
    var $this = e.findElement(), href;
    var $target = $$($this.readAttribute("data-target") || (href = $this.readAttribute("href")) && href.replace(/.*(?=#[^\s]+$)/, "")).first();
    var options = {};
    var to = $this.readAttribute("data-slide");
    var slideIndex;
    $target.retrieve("bootstrap:carousel")[to]();
    if ($this.hasAttribute("data-slide-to")) {
      slideIndex = $this.readAttribute("data-slide-to");
      $target.retrieve("bootstrap:carousel").pause().to(slideIndex).cycle();
    }
    e.stop();
  });
  $$('[data-ride="carousel"]').each(function(element) {
    new BootStrap.Carousel(element);
  });
});
"use strict";
if (BootStrap === undefined) {
  var BootStrap = {};
}
BootStrap.Button = Class.create({initialize:function(element, options) {
  this.$element = $(element);
  this.$element.store("bootstrap:button", this);
  this.options = {loadingText:"loading..."};
  if (typeof options == "object") {
    Object.extend(this.options, options);
  } else {
    if (typeof options != "undefined" && options == "toggle") {
      this.toggle();
    } else {
      if (typeof options != "undefined") {
        this.setState(options);
      }
    }
  }
}, setState:function(state) {
  var d = "disabled";
  var $el = this.$element;
  var val = $el.match("input") ? "value" : "innerHTML";
  state = state + "Text";
  if (!$el.hasAttribute("data-reset-text")) {
    $el.writeAttribute("data-reset-text", $el[val]);
  }
  $el[val] = $el.readAttribute("data-" + state.underscore().dasherize()) || this.options && this.options[state] || "";
  setTimeout(function() {
    state == "loadingText" ? $el.addClassName(d).writeAttribute(d, true) : $el.removeClassName(d).writeAttribute(d, false);
  }, 0);
}, toggle:function() {
  var $parent = this.$element.up('[data-toggle="buttons"]');
  if ($parent !== undefined) {
    var $input = this.$element.down("input");
    $input.writeAttribute("checked", !this.$element.hasClassName("active"));
    if (Event.simulate) {
      $input.simulate("change");
    }
    if ($input.readAttribute("type") === "radio") {
      $parent.select(".active").invoke("removeClassName", "active");
    }
  }
  this.$element.toggleClassName("active");
}});
document.observe("dom:loaded", function() {
  $$("[data-toggle^=button]").invoke("observe", "click", function(e) {
    var $btn = e.findElement();
    if (!$btn.hasClassName("btn")) {
      $btn = $btn.up(".btn");
    }
    new BootStrap.Button($btn, "toggle");
    e.preventDefault();
  });
});
"use strict";
if (BootStrap === undefined) {
  var BootStrap = {};
}
BootStrap.Collapse = Class.create({initialize:function(element, options) {
  this.$element = $(element);
  this.$element.store("bootstrap:collapse", this);
  this.options = {toggle:true};
  Object.extend(this.options, options);
  if (this.options.parent) {
    this.$parent = $(this.options.parent);
  }
  var dimension = this.dimension();
  this.dim_value = this.$element["get" + dimension.capitalize()]();
  this.dim_object = {};
  this.dim_object[dimension] = this.dim_value + "px";
  this.$element.setStyle(this.dim_object);
  this.clean_style = {};
  this.clean_style[dimension] = "";
  if (this.options.toggle) {
    this.toggle();
  }
}, dimension:function() {
  var hasWidth = this.$element.hasClassName("width");
  return hasWidth ? "hidth" : "height";
}, show:function() {
  if (this.transitioning || this.$element.hasClassName("in")) {
    return;
  }
  var startEvent = this.$element.fire("bootstrap:show");
  if (startEvent.defaultPrevented) {
    return;
  }
  var actives = this.$parent && this.$parent.select("> .panel > .in");
  if (actives && actives.length) {
    actives.each(function(el) {
      var bootstrapobject = el.retrieve("bootstrap:collapse");
      if (bootstrapobject && bootstrapobject.transitioning) {
        return;
      }
      bootstrapobject.hide();
    });
  }
  var dimension = this.dimension();
  this.$element.setStyle(this.clean_style);
  this.transitioning = 1;
  var complete = function() {
    this.$element.removeClassName("collapsing").addClassName("in");
    this.$element.setStyle(this.dim_object);
    this.transitioning = 0;
    this.$element.fire("bootstrap:shown");
    this.$element.stopObserving(BootStrap.transitionendevent, complete);
  }.bind(this);
  if (BootStrap.handleeffects == "css") {
    this.$element.observe(BootStrap.transitionendevent, complete);
    this.$element.removeClassName("collapse").addClassName("collapsing");
    setTimeout(function() {
      this.$element.setStyle(this.dim_object);
    }.bind(this), 0);
  } else {
    if (BootStrap.handleeffects == "effect" && typeof Effect !== "undefined" && typeof Effect.BlindDown !== "undefined") {
      this.$element.blindDown({duration:0.350, beforeStart:function(effect) {
        effect.element.hide();
        this.$element.removeClassName("collapse");
        effect.element.addClassName("in");
      }.bind(this), afterFinish:function(effect) {
        complete();
      }.bind(this)});
    } else {
      setTimeout(function() {
        complete();
      }, 350);
    }
  }
}, hide:function() {
  if (this.transitioning || !this.$element.hasClassName("in")) {
    return;
  }
  var startEvent = this.$element.fire("bootstrap:hide");
  if (startEvent.defaultPrevented) {
    return;
  }
  var dimension = this.dimension();
  var complete = function() {
    this.transitioning = 0;
    this.$element.fire("bootstrap:hidden");
    this.$element.removeClassName("collapsing").addClassName("collapse");
    this.$element.setStyle(this.dim_object);
    this.$element.stopObserving(BootStrap.transitionendevent, complete);
  }.bind(this);
  if (BootStrap.handleeffects == "css") {
    this.$element.observe(BootStrap.transitionendevent, complete);
    this.$element.addClassName("collapsing").removeClassName("in");
    setTimeout(function() {
      this.$element.setStyle(this.clean_style);
    }.bind(this), 0);
  } else {
    if (BootStrap.handleeffects == "effect" && typeof Effect !== "undefined" && typeof Effect.BlindUp !== "undefined") {
      this.$element.blindUp({duration:0.350, afterFinish:function(effect) {
        effect.element.removeClassName("in");
        effect.element.show();
        complete();
      }.bind(this)});
    } else {
      complete();
    }
  }
}, toggle:function() {
  this[this.$element.hasClassName("in") ? "hide" : "show"]();
}});
document.observe("dom:loaded", function() {
  $$('[data-toggle="collapse"]').each(function(elm) {
    var target = elm.readAttribute("data-target");
    target = $(target) || $$(target).first();
    if (!target) {
      return;
    }
    var options = {toggle:false};
    if (elm.hasAttribute("data-parent")) {
      options.parent = e.readAttribute("data-parent").replace("#", "");
    }
    if (target.hasClassName("in")) {
      target.addClassName("collapsed");
    } else {
      target.removeClassName("collapsed");
    }
    new BootStrap.Collapse(target, options);
  });
  document.on("click", '[data-toggle="collapse"]', function(e) {
    e.stop();
    var elm = e.findElement("[data-toggle]");
    var target = elm.readAttribute("data-target");
    target = $(target) || $$(target).first();
    if (!target) {
      return;
    }
    target.retrieve("bootstrap:collapse").toggle();
  });
});
"use strict";
if (BootStrap === undefined) {
  var BootStrap = {};
}
BootStrap.ScrollSpy = Class.create({initialize:function(element, options) {
  element = $(element);
  element.store("bootstrap:scrollspy", this);
  this.options = {offset:30};
  if (element.hasAttribute("data-target")) {
    this.options.target = element.readAttribute("data-target");
  }
  var $element = element.match("body") ? window : element;
  var href;
  Object.extend(this.options, options);
  this.$scrollElement = $element.observe("scroll", this.process.bind(this));
  this.selector = (this.options.target || (href = element.readAttribute("href")) && href.replace(/.*(?=#[^\s]+$)/, "") || "") + " .nav li > a";
  this.$body = $$("body").first();
  this.refresh();
  this.process();
}, refresh:function() {
  var self = this;
  var $targets;
  this.offsets = [];
  this.targets = [];
  $targets = this.$body.select(this.selector).map(function(t) {
    var $el = t;
    var href = $el.readAttribute("data-target") || $el.readAttribute("href");
    var $href = /^#\w/.test(href) && $$(href).first();
    return $href && [$href.viewportOffset().top - $href.getHeight() + (this.$scrollElement != window && this.$scrollElement.cumulativeScrollOffset().top), href] || null;
  }, this).without(false, null).sort(function(a, b) {
    return a - b;
  }).each(function(v) {
    this.offsets.push(v[0]);
    this.targets.push(v[1]);
  }, this);
}, process:function() {
  var scrollTop = this.$scrollElement.cumulativeScrollOffset().top + this.options.offset;
  var scrollHeight = this.$scrollElement.scrollHeight || this.$body.scrollHeight;
  var maxScroll = scrollHeight - this.$scrollElement.getHeight();
  var offsets = this.offsets;
  var targets = this.targets;
  var activeTarget = this.activeTarget;
  var i;
  if (scrollTop >= maxScroll) {
    return activeTarget != (i = targets.last()) && this.activate(i);
  }
  for (i = offsets.length; i--;) {
    activeTarget != targets[i] && scrollTop >= offsets[i] && (!offsets[i + 1] || scrollTop <= offsets[i + 1]) && this.activate(targets[i]);
  }
}, activate:function(target) {
  var active, selector;
  this.activeTarget = target;
  $$(this.options.target).length > 0 ? $$(this.options.target).first().select(".active").invoke("removeClassName", "active") : "";
  selector = this.selector + '[data-target="' + target + '"],' + this.selector + '[href="' + target + '"]';
  active = $$(selector).first().up("li").addClassName("active");
  if (active.up(".dropdown-menu") !== undefined) {
    active = active.up("li.dropdown").addClassName("active");
  }
  active.fire("bootstrap:activate");
}});
Event.observe(window, "load", function() {
  $$('[data-spy="scroll"]').each(function(element) {
    new BootStrap.ScrollSpy(element);
  });
});
"use strict";
if (BootStrap === undefined) {
  var BootStrap = {};
}
BootStrap.Dropdown = Class.create({initialize:function(element) {
  element.store("bootstrap:dropdown", this);
  var $el = $(element).on("click", this.toggle);
  $$("html")[0].on("click", function() {
    $el.up().removeClassName("open");
  });
}, toggle:function(e) {
  var $this = $(this), $parent, isActive;
  if ($this.hasClassName("disabled") || $this.readAttribute("disabled") == "disabled") {
    return;
  }
  $parent = BootStrap.Dropdown.prototype.getParent($this);
  isActive = $parent.hasClassName("open");
  BootStrap.Dropdown.prototype.clearMenus();
  if (!isActive) {
    if ("ontouchstart" in document.documentElement) {
      var backdrop = new Element("div", {"class":"dropdown-backdrop"});
      backdrop.observe("click", BootStrap.Dropdown.prototype.clearMenus);
      $this.insert({"before":backdrop});
    }
    $parent.toggleClassName("open");
  }
  $this.focus();
  e.stop();
}, keydown:function(e) {
  var $this, $items, $active, $parent, isActive, index;
  if (!/(38|40|27)/.test(e.keyCode)) {
    return;
  }
  $this = $(this);
  e.preventDefault();
  e.stopPropagation();
  if ($this.hasClassName("disabled") || $this.readAttribute("disabled") == "disabled") {
    return;
  }
  $parent = BootStrap.Dropdown.prototype.getParent($this);
  isActive = $parent.hasClassName("open");
  if (!isActive || isActive && e.keyCode == Event.KEY_ESC) {
    if (e.which == Event.KEY_ESC) {
      $parent.select("[data-toggle=dropdown]")[0].focus();
    }
    return $this.click();
  }
  $items = $parent.select("[role=menu] li:not(.divider) a");
  if (!$items.length) {
    return;
  }
  index = -1;
  $items.each(function(item, i) {
    item.match(":focus") ? index = i : "";
  });
  if (e.keyCode == Event.KEY_UP && index > 0) {
    index--;
  }
  if (e.keyCode == Event.KEY_DOWN && index < $items.length - 1) {
    index++;
  }
  if (!~index) {
    index = 0;
  }
  $items[index].focus();
}, clearMenus:function() {
  $$(".dropdown-backdrop").invoke("remove");
  $$("[data-toggle=dropdown]").each(function(i) {
    BootStrap.Dropdown.prototype.getParent(i).removeClassName("open");
  });
}, getParent:function(element) {
  var selector = element.readAttribute("data-target"), $parent;
  if (!selector) {
    selector = element.readAttribute("href");
    selector = selector && /#/.test(selector) && selector.replace(/.*(?=#[^\s]*$)/, "") && selector != "#";
  }
  $parent = selector && $$(selector);
  if (!$parent || !$parent.length) {
    $parent = element.up();
  }
  return $parent;
}});
document.observe("dom:loaded", function() {
  document.observe("click", BootStrap.Dropdown.prototype.clearMenus);
  $$(".dropdown form").invoke("observe", "click", function(e) {
    e.stop();
  });
  $$("[data-toggle=dropdown]").invoke("observe", "click", BootStrap.Dropdown.prototype.toggle);
  $$("[data-toggle=dropdown]" + ", [role=menu]").invoke("observe", "keydown", BootStrap.Dropdown.prototype.keydown);
});

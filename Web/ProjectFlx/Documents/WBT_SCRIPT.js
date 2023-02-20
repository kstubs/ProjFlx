var wbt = wbt || {};

// extend form objects
wbt.FormMethodAddons = {
	findFirstBlankElement: function(form) {
        var elements = $(form).getElements().findAll(function(element) {
          return 'hidden' != element.type && !element.disabled && Form.Element.getValue(element).blank();
        });
        var firstByIndex = elements.findAll(function(element) {
          return element.hasAttribute('tabIndex') && element.tabIndex >= 0;
        }).sortBy(function(element) { return element.tabIndex }).first();

        return firstByIndex ? firstByIndex : elements.find(function(element) {
          return /^(?:input|select|textarea)$/i.test(element.tagName);
      });
    },
    focusFirstBlankElement: function(form) {
        form = $(form);
        var element = Form.findFirstBlankElement(form);
        if (element) element.activate();
        return form;        
    }
}
Object.extend(Form, wbt.FormMethodAddons);

wbt.Toggle = function(elm) {
    elm.on('click', function(e) {
    var elm = e.findElement('[wbt-toggle]');
    if(!elm) return true;
    e.stop();
    var id = elm.readAttribute('wbt-toggle');
    var effects = (elm.readAttribute('wbt-toggle-effects') === null) ? true : elm.readAttribute('wbt-toggle-effects') == true;
    var toggleElm = $(id);
    if(toggleElm) {
        if(toggleElm.visible()) {
            if(effects) {
                new Effect.SlideUp(toggleElm, { duration: wbt.Defaults.Effect.TransitionFast});                        
            } else {
                toggleElm.hide();
            }
            document.fire('wbt:toggle-hidden', toggleElm);
        } else {
            if(effects) {
                new Effect.SlideDown(toggleElm, { duration: wbt.Defaults.Effect.TransitionNormal});
            } else {
                toggleElm.show();
            }
            document.fire('wbt:toggle-visible', toggleElm);
        }
    }

});

}

wbt.TargetDefaultFeatures = {
        width:500,
        height:400,
        titlebar:'no',
        location:'no',
        scrollbar:'yes',
        menubar:'no',
        toolbar:'no',
        status:'no',
        personalbar:'no',
        toolbar:'no'
}

wbt.TargetNewWin = function() {
    var targetFunc = function(e) {
        var elm = e.findElement('[wbt-targeting]');        
        var features = wbt.TargetDefaultFeatures;
        features.att = {};
        features.att.parsed = elm.readAttribute('wbt-targeting-features');
        if(features.att.parsed) {
            features.att.vals = features.att.parsed.split(',').collect(function(p) { 
                var h = $H();
                var a = p.split('=');
                h.set(a[0], isNaN(Number(a[1])) ? a[1] : Number(a[1]));
                var obj = h.toObject();

                if(obj[a[0]] && 
                    Object.isString(obj[a[0]]) && 
                        obj[a[0]].toUpperCase() === 'MAX') {
                    switch(a[0]) {
                        case 'width':
                            obj[a[0]] == window.innerWidth;
                        break;
                        case 'height':
                            obj[a[0]] == window.innerHeight;
                        break;
                    }
                }
                return obj;
            });

            features.att.vals.each(function(val) {
                features[Object.keys(val)[0]] = Object.values(val)[0];
            });
        }


        var dualScreenLeft = window.screenLeft != undefined ? window.screenLeft : screen.left;
        var dualScreenTop = window.screenTop != undefined ? window.screenTop : screen.top;

        var width = window.innerWidth ? window.innerWidth : document.documentElement.clientWidth ? document.documentElement.clientWidth : screen.width;
        var height = window.innerHeight ? window.innerHeight : document.documentElement.clientHeight ? document.documentElement.clientHeight : screen.height;

        features.left = ((width / 2) - (features.width / 2)) + dualScreenLeft;
        features.top = ((height / 2) - (features.height / 2)) + dualScreenTop;

        var tmp = Object.toQueryString(features).split('&').join(',');
        if(elm === null) return;
        elm.writeAttribute('target', elm.readAttribute('wbt-targeting'));
        var win = window.open(elm.readAttribute('wbt-targeting'), "about:blank",tmp);
        win.document.title = "WBT New Win";
        return true;
    };
    $$('[wbt-targeting]').each(function(elm) {
        switch(elm.tagName)
        {
            case 'FORM':
                elm.on('submit', targetFunc.bindAsEventListener())
            break;
            default:
                elm.on('click', targetFunc.bindAsEventListener());
            break;

        }
    });

}

document.observe('dom:loaded', function() {
        // $$('.navbar-collapse').each(function(nav) {
        //     $(nav).on('click','a',function() {
        //         var collapse = this.retrieve('bootstrap:collapse');
        //         if(collapse) {
        //             collapse.toggle();            
        //         }
        //     });
        // });
        $$('*[wbt-toggle]').each(wbt.Toggle);

});

wbt.Viewport = {
};
wbt.Offsets = {
};
wbt.queryvars = {
    "value": ""
};
wbt.Defaults = {
    Effect: {
        TransitionFast: (.25),
        TransitionNormal: (.5),
        TransitionSlow: 1,
    }
}

wbt.SimpleForms = {
    accept_max_keys: [Event.KEY_DELETE, Event.KEY_BACKSPACE, Event.KEY_ESC, Event.KEY_LEFT, Event.KEY_HOME, Event.KEY_END, Event.KEY_RIGHT],
    attachSimpleFormListeners: function (form) {
        if (!(form || this.form)) {
            return;
        }
        this.form.on('keyup', this._keyed.bindAsEventListener(this));
        this.form.on('keydown', this._keyed.bindAsEventListener(this));
        this.form.on('keypress', this._keyed.bindAsEventListener(this));
        this.form.getInputs().each(function (input) {
            input.on('blur', this._blured.bindAsEventListener(this));

            var maxlen = input.readAttribute('data-max-len');
            if(Number(maxlen)) {
                var diff = maxlen - input.getValue().len;
                if(!isNaN(diff)) {
                    var label = input.next('span.max-label');
                    if(label) {
                        label.update(diff);
                    }
                }
            }
        },this);
    },
    
    _keyed: function (e) {
        var elm = e.findElement('input');
        if (elm == null) {
            return;
        }
        
        var obj = $F(elm);
        if(obj === null)
            return;
        
        var len = $F(elm).length
        var maxlen = elm.readAttribute('data-max-len');
        var diff = maxlen - len;
        var label = elm.next('span.max-label');
        
        if (maxlen == null) {
            return;
        }
        switch (e.type) {
            case 'keydown':
            if (diff < 1 && ! this.accept_max_keys.include(e.keyCode)) {
                if(label != null) {
                    label.update('0');
                    label.addClassName('maxed');
                }
                e.stop();                
            } else {
                if(label != null) {
                    label.update(diff);
                    label.removeClassName('maxed');
                }
            }
            break;
            case 'keypress':
            if(label != null) {
                label.update(diff);
                label.removeClassName('maxed');
            }
            break;
            case 'keyup':            
            break;
        }
    },
    
    _blured: function (e) {
        var elm = e.findElement();
        if (elm) {
            var blankname = elm.readAttribute('name') + '_isBlank';     // blank name field
            var blankelm = elm.up('form').down('input[name=' + blankname + ']');
            // check if blank and diffierent than original
            if(elm.readAttribute('data-orig') && !elm.readAttribute('data-orig').blank()) {
                var orig = elm.readAttribute('data-orig');
                if(elm.getValue().blank() && !blankelm) {
                    elm.up('form').insert('<input type=\'hidden\' name=\'' + blankname + '\' value=\'true\'/>');
                } else {
                    if(blankelm)
                        $(blankelm).remove();
                }
            }


            var maxlen = elm.readAttribute('data-max-len');
            if (maxlen == null) {
                return;
            }
            var label = elm.next('span.max-label');
            if(label) {
                var data = $F(elm);
                if (data != null) {
                    var len = data.length;
                    var tot = maxlen - len;                
                    if (!(isNaN(tot)) && tot < 1) {
                        elm.setValue(data.substring(0, maxlen));
                        label.update('0');
                        label.addClassName('maxed');
                    }
                }
            }
        }
    }
}

wbt.DisplaySize = {
    Size: null, Viewport: {
    }
};

wbt.calcBrowser = function () {
    wbt.Viewport = document.viewport.getDimensions();
    if (document.loaded) {
        wbt.Layout = $(document.body).getLayout();
        wbt.Offsets = document.viewport.getScrollOffsets();
        
        if (wbt.Viewport.width < 402) {
            wbt.DisplaySize = 'xxs';
        } else if (wbt.Viewport.width >= 402 && wbt.Viewport.width < 768) {
            wbt.DisplaySize = 'xs';
        } else if (wbt.Viewport.width >= 768 && wbt.Viewport.width < 992) {
            wbt.DisplaySize = 'sm';
        } else if (wbt.Viewport.width >= 992 && wbt.Viewport.width < 1200) {
            wbt.DisplaySize = 'md';
        } else if (wbt.Viewport.width >= 1200) {
            wbt.DisplaySize = 'lg';
        }
    }
};

document.on('dom:loaded', function () {
    wbt.calcBrowser();
    wbt.TargetNewWin();
});
wbt.calcBrowser();

wbt.__scrollHandler = function () {
    if (wbt.__wait_scroll) {
        clearTimeout(wbt.__wait_scroll);
    }
    wbt.__wait_scroll = wbt.__scroll_Stop.delay(0.1);
};

wbt.__scroll_Stop = function () {
    wbt.calcBrowser();
    document.fire('wbt:ScrollEnded');
};

wbt.__windowResizeHandler = function () {
    if (wbt.__wait_resize) {
        clearTimeout(wbt.__wait_resize);
    }
    wbt.__wait_resize = wbt.__resize_Stop.delay(0.3);
};

wbt.__resize_Stop = function () {
    wbt.calcBrowser();
    document.fire('wbt:ResizeEnded');
};

Event.observe(window, 'scroll', wbt.__scrollHandler);
Event.observe(window, 'resize', wbt.__windowResizeHandler);

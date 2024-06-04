var wbt = window.wbt || {};

wbt.CountDown = Class.create({
    initialize(layer) {
        this.layer = $(layer);
        this._setupContainer;
        this._nowtime = Number(this.layer.readAttribute('data-unix-time'))
        this._futuretime = Number(this.layer.readAttribute('data-valid-on'));

        this._setup(wbt.DisplaySize === 'xxs');
        this._countDown();
    },
    _setup: function(isMobile) {
        if(isMobile)
            this.layer.addClassName('xs');

        var element = this.layer;
        var elements = [];
        var elm = new Element('div',{class:'wbt-seconds  bg-dark text-light rounded-end'});
        elm.insert('<label>Seconds</label>')
        elm.insert('<span></span>');
        elements.push(elm);
        
        elm = new Element('div',{class:'wbt-minutes  bg-dark text-light'});
        elm.insert('<label>Minutes</label>')
        elm.insert('<span></span>');
        elements.push(elm);
        
        elm = new Element('div',{class:'wbt-hours  bg-dark text-light'});
        elm.insert('<label>Hours</label>')
        elm.insert('<span></span>');
        elements.push(elm);
        
        elm = new Element('div',{class:'wbt-days bg-dark  text-light rounded-start', 'data-val':0});
        elm.insert('<label>Days</label>')
        elm.insert('<span></span>');
        elements.push(elm);

        if(isMobile)
            elements.reverse();

        elements.each(function(item) {            
            $(this.layer).insert(item);
        }, this);
    },
    _countDown: function() {
        var funcExpired = function() {
            this.__z_timer_Draw.stop();
            location.reload(true);
        }.bind(this);

        var funcTimes = function(nowtime, futuretime) {

            var delta = futuretime - nowtime;
            var smap = {
                inminutes : 60,
                inhours : 60 * 60,
                indays : 60 * 60 * 24
            }
            var ttime = { };
            ttime.days = Math.floor((delta / smap.indays));
            ttime.hours = Math.floor((delta - (ttime.days * smap.indays)) / smap.inhours);
            ttime.minutes = Math.floor((delta - ((ttime.days * smap.indays) + (ttime.hours * smap.inhours))) / smap.inminutes);
            ttime.seconds = Math.floor(delta - ((ttime.days * smap.indays) + (ttime.hours * smap.inhours) + (ttime.minutes * smap.inminutes)));

            return ttime;
        };

        var $days = this.layer.down('.wbt-days span');
        var $hours = this.layer.down('.wbt-hours span');
        var $minutes = this.layer.down('.wbt-minutes span');
        var $seconds = this.layer.down('.wbt-seconds span');

        
        var futuretime = this._futuretime;
        var ttime = funcTimes(this._nowtime, futuretime);

        var draw = function() {
            $days.update(ttime.days);
            $hours.update(ttime.hours);
            $minutes.update(ttime.minutes);
            $seconds.update(ttime.seconds);

            if(ttime.days + ttime.hours + ttime.minutes + ttime.seconds === 0) {
                funcExpired();
            }

            ttime = funcTimes(this._nowtime, futuretime--);
        }.bind(this);

        this.__z_timer_Draw = new PeriodicalExecuter(draw, 1);

    }
})

document.observe('dom:loaded', function() {
/*    wbt.Countdown = 'initialzed';


    var funcEveryElement = function(element) {
    

        var ttime = funcTimes(nowtime, futuretime);

        var elm = new Element('div',{class:'wbt-seconds'});
        elm.insert('<label>Seconds</label>')
        elm.insert('<span>#{seconds}</span>'.interpolate(ttime));
        $(element).insert(elm);
        
        elm = new Element('div',{class:'wbt-minutes'});
        elm.insert('<label>Minutes</label>')
        elm.insert('<span>#{minutes}</span>'.interpolate(ttime));
        $(element).insert(elm);
        
        elm = new Element('div',{class:'wbt-hours'});
        elm.insert('<label>Hours</label>')
        elm.insert('<span>#{hours}</span>'.interpolate(ttime));
        $(element).insert(elm);
        
        elm = new Element('div',{class:'wbt-days', 'data-val':0});
        elm.insert('<label>Days</label>')
        elm.insert('<span>#{days}</span>'.interpolate(ttime));
        $(element).insert(elm);
    };
    
*/    
    $$('.wbt-countdown').each(function(layer) {
        new wbt.CountDown(layer)
    });

});
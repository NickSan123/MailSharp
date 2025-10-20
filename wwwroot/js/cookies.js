window.mailsharp = {
    setCookie: function (name, value, maxAgeSeconds) {
        var expires = "";
        if (maxAgeSeconds && Number.isFinite(maxAgeSeconds)) {
            var d = new Date();
            d.setTime(d.getTime() + maxAgeSeconds * 1000);
            expires = ";expires=" + d.toUTCString();
        }
        var secure = location.protocol === "https:" ? ";Secure" : "";
        var sameSite = ";SameSite=Strict";
        document.cookie = name + "=" + encodeURIComponent(value) + expires + ";path=/" + sameSite + secure;
    }
};
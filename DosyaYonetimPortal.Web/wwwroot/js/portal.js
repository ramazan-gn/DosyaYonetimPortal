(function (global) {
    'use strict';

    var STORAGE_KEY = 'dosya_portal_token';

    function baseUrl() {
        return (global.APP_CONFIG && global.APP_CONFIG.apiBaseUrl) || '';
    }

    function token() {
        return sessionStorage.getItem(STORAGE_KEY);
    }

    function setToken(t) {
        if (t) sessionStorage.setItem(STORAGE_KEY, t);
        else sessionStorage.removeItem(STORAGE_KEY);
    }

    function authHeader() {
        var t = token();
        return t ? { Authorization: 'Bearer ' + t } : {};
    }

    function ajax(options) {
        var headers = $.extend({}, options.headers || {}, authHeader());
        return $.ajax($.extend({}, options, { headers: headers }));
    }

    function apiUrl(path) {
        var b = baseUrl().replace(/\/$/, '');
        var p = path.indexOf('/') === 0 ? path : '/' + path;
        return b + p;
    }

    function downloadFile(id, originalName) {
        return $.ajax({
            url: apiUrl('/api/files/' + id + '/download'),
            method: 'GET',
            headers: authHeader(),
            xhrFields: { responseType: 'blob' }
        }).done(function (blob) {
            var url = window.URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = originalName || 'dosya';
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(url);
        });
    }

    function formatBytes(bytes) {
        if (bytes === 0) return '0 B';
        var k = 1024;
        var sizes = ['B', 'KB', 'MB', 'GB'];
        var i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    function navRefreshed() {
        if (global.jQuery) {
            global.jQuery(document).trigger('portal:navrefreshed');
        }
    }

    function refreshNav() {
        var $auth = $('#nav-auth');
        var $guest = $('#nav-guest');
        var $admin = $('#nav-admin');
        if (!$auth.length) {
            navRefreshed();
            return;
        }

        var t = token();
        if (!t) {
            global.Portal.userId = null;
            global.Portal.roles = [];
            $guest.removeClass('d-none');
            $auth.addClass('d-none');
            $admin.addClass('d-none');
            navRefreshed();
            return;
        }

        ajax({ url: apiUrl('/api/auth/me'), method: 'GET' })
            .done(function (data) {
                $('#nav-user-email').text(data.email || '');
                global.Portal.userId = data.userId || null;
                global.Portal.roles = data.roles || [];
                $guest.addClass('d-none');
                $auth.removeClass('d-none');
                var roles = global.Portal.roles;
                var isAdmin = roles.indexOf('Admin') >= 0;
                if (isAdmin) $admin.removeClass('d-none');
                else $admin.addClass('d-none');
                navRefreshed();
            })
            .fail(function () {
                setToken(null);
                global.Portal.userId = null;
                global.Portal.roles = [];
                $guest.removeClass('d-none');
                $auth.addClass('d-none');
                $admin.addClass('d-none');
                navRefreshed();
            });
    }

    global.Portal = {
        baseUrl: baseUrl,
        apiUrl: apiUrl,
        token: token,
        setToken: setToken,
        userId: null,
        roles: [],
        ajax: ajax,
        downloadFile: downloadFile,
        formatBytes: formatBytes,
        refreshNav: refreshNav
    };

    $(function () {
        // Diğer sayfa betiklerinin portal:navrefreshed dinleyicisini eklemesine izin ver
        setTimeout(function () {
            Portal.refreshNav();
        }, 0);
        $(document).on('click', '#btn-logout', function (e) {
            e.preventDefault();
            Portal.setToken(null);
            Portal.userId = null;
            Portal.roles = [];
            window.location.href = '/';
        });
    });
})(window);

(function () {
    'use strict';

    var deferredInstallPrompt = null;
    var indicator = null;
    var installBanner = null;
    var operationalPage = document.body?.dataset?.wmsOperational === 'true';

    function ensureIndicator() {
        if (!operationalPage) return null;
        if (indicator) return indicator;
        indicator = document.createElement('div');
        indicator.id = 'wmsNetworkIndicator';
        indicator.className = 'wms-network-indicator';
        indicator.setAttribute('role', 'status');
        indicator.setAttribute('aria-live', 'polite');
        document.body.appendChild(indicator);
        return indicator;
    }

    function showNetworkState() {
        var element = ensureIndicator();
        if (!element) return;

        if (navigator.onLine) {
            element.textContent = 'Đã kết nối mạng';
            element.classList.remove('offline');
            element.classList.add('online', 'visible');
            window.setTimeout(function () {
                element.classList.remove('visible');
            }, 2200);
            return;
        }

        element.textContent = 'Mất kết nối mạng. Thao tác nghiệp vụ cần gửi khi có mạng.';
        element.classList.remove('online');
        element.classList.add('offline', 'visible');
    }

    function createInstallBanner() {
        if (!operationalPage) return null;
        if (installBanner) return installBanner;
        installBanner = document.createElement('div');
        installBanner.className = 'wms-install-banner';
        installBanner.innerHTML =
            '<div><strong>Cài WMS Pro lên màn hình chính</strong><span>Mở nhanh như ứng dụng khi thao tác trong kho.</span></div>' +
            '<div class="wms-install-actions">' +
            '<button type="button" class="btn btn-primary" id="wmsInstallAccept">Cài đặt</button>' +
            '<button type="button" class="btn btn-secondary" id="wmsInstallDismiss">Để sau</button>' +
            '</div>';
        document.body.appendChild(installBanner);

        document.getElementById('wmsInstallAccept')?.addEventListener('click', function () {
            if (!deferredInstallPrompt) return;
            deferredInstallPrompt.prompt();
            deferredInstallPrompt.userChoice.finally(function () {
                deferredInstallPrompt = null;
                installBanner.classList.remove('visible');
            });
        });

        document.getElementById('wmsInstallDismiss')?.addEventListener('click', function () {
            installBanner.classList.remove('visible');
            sessionStorage.setItem('wms_pwa_install_dismissed', 'true');
        });

        return installBanner;
    }

    function showInstallPrompt(event) {
        event.preventDefault();
        deferredInstallPrompt = event;
        if (!operationalPage || sessionStorage.getItem('wms_pwa_install_dismissed') === 'true') return;
        document.body?.classList?.add('wms-install-ready');
    }

    function registerServiceWorker() {
        if (!('serviceWorker' in navigator)) return;
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/service-worker.js')
                .then(function (registration) {
                    registration.addEventListener('updatefound', function () {
                        var worker = registration.installing;
                        if (!worker) return;
                        worker.addEventListener('statechange', function () {
                            if (worker.state === 'installed' && navigator.serviceWorker.controller && window.enterpriseNotify) {
                                window.enterpriseNotify({
                                    title: 'Đã có phiên bản web mới',
                                    text: 'Tải lại trang để dùng phiên bản mới nhất.',
                                    icon: 'info',
                                    showConfirmButton: true
                                });
                            }
                        });
                    });
                })
                .catch(function () {
                    // Service worker chỉ là lớp tiện ích; nếu trình duyệt chặn thì web vẫn chạy trực tiếp.
                });
        });
    }

    window.addEventListener('beforeinstallprompt', showInstallPrompt);
    window.addEventListener('online', function () {
        showNetworkState();
        if (window.wmsOfflineQueue && typeof window.wmsOfflineQueue.flush === 'function') {
            window.wmsOfflineQueue.flush();
        }
    });
    window.addEventListener('offline', showNetworkState);
    document.addEventListener('DOMContentLoaded', function () {
        if (!navigator.onLine) showNetworkState();
    });
    registerServiceWorker();

    window.wmsPwa = {
        showNetworkState: showNetworkState,
        showInstallPrompt: function () {
            if (deferredInstallPrompt) createInstallBanner()?.classList.add('visible');
        }
    };
})();

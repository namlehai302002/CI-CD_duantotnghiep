(function () {
    'use strict';

    function ready(fn) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', fn, { once: true });
            return;
        }
        fn();
    }

    function resolveLoadingTarget(target) {
        if (!target) return document.body;
        if (typeof target === 'string') return document.querySelector(target) || document.body;
        return target;
    }

    function isButtonLike(element) {
        if (!element || !element.matches) return false;
        return element.matches('button, input[type="submit"], input[type="button"], .btn, [role="button"]');
    }

    function isFieldLike(element) {
        if (!element || !element.matches) return false;
        return element.matches('input:not([type="submit"]):not([type="button"]), textarea, select');
    }

    function createLoadingOverlay(text) {
        var overlay = document.createElement('div');
        overlay.className = 'wms-loading-overlay';
        overlay.setAttribute('role', 'status');
        overlay.setAttribute('aria-live', 'polite');
        overlay.innerHTML = '<span class="wms-loading-spinner" aria-hidden="true"></span><span class="wms-loading-text"></span>';
        overlay.querySelector('.wms-loading-text').textContent = text || '\u0110ang x\u1eed l\u00fd...';
        return overlay;
    }

    function beginLoading(target, options) {
        var settings = options || {};
        var element = resolveLoadingTarget(target);
        var delay = Number.isFinite(settings.delay) ? settings.delay : 700;
        var handle = {
            target: element,
            timer: null,
            visible: false,
            overlay: null,
            isButton: isButtonLike(element),
            isField: isFieldLike(element),
            originalHtml: element && element.innerHTML,
            originalDisabled: element && 'disabled' in element ? element.disabled : null,
            hadRegionClass: element && element.classList ? element.classList.contains('wms-loading-region') : false
        };

        if (!element) return handle;

        element.setAttribute('aria-busy', 'true');
        if (handle.isButton && settings.disable !== false && 'disabled' in element) {
            element.disabled = true;
            element.setAttribute('aria-disabled', 'true');
        }

        handle.timer = window.setTimeout(function () {
            handle.visible = true;
            element.classList.add('is-wms-loading');

            if (handle.isButton) {
                element.classList.add('enterprise-submit-loading');
                if (settings.text) {
                    element.textContent = '';
                    var label = document.createElement('span');
                    label.textContent = settings.text;
                    element.appendChild(label);
                }
                return;
            }

            if (handle.isField) {
                element.classList.add('wms-loading-field');
                return;
            }

            element.classList.add('wms-loading-region');
            handle.overlay = createLoadingOverlay(settings.text);
            element.appendChild(handle.overlay);
        }, Math.max(0, delay));

        return handle;
    }

    function endLoading(handle) {
        if (!handle || !handle.target) return;
        var element = handle.target;
        if (handle.timer) window.clearTimeout(handle.timer);

        element.removeAttribute('aria-busy');
        element.classList.remove('is-wms-loading');

        if (handle.isButton) {
            element.classList.remove('enterprise-submit-loading');
            if (handle.originalHtml != null) element.innerHTML = handle.originalHtml;
            if ('disabled' in element && handle.originalDisabled != null) {
                element.disabled = handle.originalDisabled;
            }
            element.removeAttribute('aria-disabled');
            return;
        }

        if (handle.isField) {
            element.classList.remove('wms-loading-field');
            return;
        }

        if (handle.overlay && handle.overlay.parentNode) {
            handle.overlay.parentNode.removeChild(handle.overlay);
        }
        if (!handle.hadRegionClass) {
            element.classList.remove('wms-loading-region');
        }
    }

    function withBusy(target, taskOrPromise, options) {
        var handle = beginLoading(target, options);
        try {
            var result = typeof taskOrPromise === 'function' ? taskOrPromise() : taskOrPromise;
            return Promise.resolve(result).finally(function () {
                endLoading(handle);
            });
        } catch (error) {
            endLoading(handle);
            throw error;
        }
    }

    window.wmsLoading = {
        begin: beginLoading,
        end: endLoading,
        withBusy: withBusy
    };

    function enhanceTables() {
        document.querySelectorAll('.main-content table').forEach(function (table) {
            if (table.closest('.print-page, .print-sheet, .label-page, .no-enterprise-enhance')) return;

            table.classList.add('enterprise-table');

            var parent = table.parentElement;
            var needsWrap = parent
                && !parent.classList.contains('table-responsive')
                && !parent.classList.contains('table-container')
                && !parent.classList.contains('yardops-table-wrap')
                && !parent.classList.contains('enterprise-table-wrap');

            if (needsWrap) {
                var wrapper = document.createElement('div');
                wrapper.className = 'enterprise-table-wrap';
                parent.insertBefore(wrapper, table);
                wrapper.appendChild(table);
            }

            var headers = Array.from(table.querySelectorAll('thead th')).map(function (th) {
                return th.textContent.trim();
            });
            if (!headers.length) return;

            table.querySelectorAll('tbody tr').forEach(function (row) {
                Array.from(row.children).forEach(function (cell, index) {
                    if (!cell.dataset.label && headers[index]) {
                        cell.dataset.label = headers[index];
                    }
                });
            });
        });
    }

    function enhanceForms() {
        document.querySelectorAll('.main-content form').forEach(function (form) {
            if (form.closest('.no-enterprise-enhance')) return;
            form.classList.add('enterprise-enhanced-form');
        });

        document.addEventListener('submit', function (event) {
            if (event.defaultPrevented) return;
            var form = event.target;
            if (!(form instanceof HTMLFormElement)) return;
            if (form.dataset.noSubmitLoading === 'true') return;
            var submitter = event.submitter;
            if (!(submitter instanceof HTMLButtonElement)) return;
            if (submitter.dataset.noSubmitLoading === 'true') return;
            beginLoading(submitter, {
                delay: Number(submitter.dataset.loadingDelay || form.dataset.loadingDelay || 700),
                text: submitter.dataset.loadingText || form.dataset.loadingText || null
            });
        }, true);
    }

    function enhanceStatusBadges() {
        document.querySelectorAll('.status-badge, .badge').forEach(function (badge) {
            var text = badge.textContent.trim().toLocaleLowerCase('vi');
            if (!text) return;
            if (/(lỗi|hủy|chặn|quá hạn|thất bại|dead)/.test(text)) badge.classList.add('badge-danger');
            else if (/(hoàn tất|đã gửi|đã xác nhận|ổn định|hoạt động|success)/.test(text)) badge.classList.add('badge-success');
            else if (/(chờ|nháp|đang|cảnh báo|warning)/.test(text)) badge.classList.add('badge-warning');
            else if (/(mới|thông tin|info)/.test(text)) badge.classList.add('badge-info');
        });
    }

    function enhanceDataWidths() {
        document.querySelectorAll('[data-progress-width], [data-segment-width]').forEach(function (element) {
            var width = element.dataset.progressWidth || element.dataset.segmentWidth;
            if (!width) return;
            element.style.width = width;
        });
    }

    window.enhanceDataWidths = enhanceDataWidths;

    function parseActionValue(value) {
        if (value == null) return value;
        if (value === 'true') return true;
        if (value === 'false') return false;
        if (/^-?\d+(\.\d+)?$/.test(value)) return Number(value);
        return value;
    }

    function parseActionArgs(element) {
        if (element.dataset.wmsJsonArgs) {
            try {
                return JSON.parse(element.dataset.wmsJsonArgs);
            } catch (error) {
                console.warn('Invalid WMS action arguments.', error);
                return [];
            }
        }

        return ['wmsArg', 'wmsArg2', 'wmsArg3', 'wmsArg4']
            .filter(function (key) { return Object.prototype.hasOwnProperty.call(element.dataset, key); })
            .map(function (key) { return parseActionValue(element.dataset[key]); });
    }

    function callNamedAction(name, args, sourceElement) {
        if (!name || typeof window[name] !== 'function') return false;
        window[name].apply(window, args || []);
        if (sourceElement) sourceElement.dispatchEvent(new CustomEvent('wms:action-called', { bubbles: true, detail: { action: name } }));
        return true;
    }

    function closeModalById(id) {
        var modal = document.getElementById(id);
        if (!modal) return;
        if (modal.classList.contains('active')) modal.classList.remove('active');
        if (modal.classList.contains('is-open')) modal.classList.remove('is-open');
        if (modal.style.display && modal.style.display !== 'none') modal.style.display = 'none';
        modal.setAttribute('aria-hidden', 'true');
    }

    function openModalById(id) {
        var modal = document.getElementById(id);
        if (!modal) return;
        modal.classList.add('active');
        if (modal.style.display === 'none') modal.style.display = 'flex';
        modal.removeAttribute('aria-hidden');
    }

    function runDataAction(element, event) {
        if (!element || element.disabled || element.getAttribute('aria-disabled') === 'true') return;

        if (element.dataset.wmsWindowAction) {
            event.preventDefault();
            var action = element.dataset.wmsWindowAction;
            if (action === 'print') window.print();
            else if (action === 'close') window.close();
            else if (action === 'back') window.history.back();
            return;
        }

        if (element.dataset.wmsNotifyTitle) {
            event.preventDefault();
            if (typeof window.enterpriseNotify === 'function') {
                window.enterpriseNotify({
                    title: element.dataset.wmsNotifyTitle,
                    text: element.dataset.wmsNotifyText || '',
                    icon: element.dataset.wmsNotifyIcon || 'info'
                });
            }
            return;
        }

        if (element.dataset.wmsClickTarget) {
            event.preventDefault();
            var target = document.querySelector(element.dataset.wmsClickTarget);
            if (target) target.click();
            return;
        }

        if (element.dataset.wmsExportTable) {
            event.preventDefault();
            callNamedAction('exportTableToExcel', [element.dataset.wmsExportTable, element.dataset.wmsExportFilename || 'wms_export'], element);
            return;
        }

        if (element.dataset.wmsCloseModal) {
            event.preventDefault();
            closeModalById(element.dataset.wmsCloseModal);
            return;
        }

        if (element.dataset.wmsOpenModal) {
            event.preventDefault();
            openModalById(element.dataset.wmsOpenModal);
            return;
        }

        if (element.dataset.wmsCallSelf) {
            event.preventDefault();
            callNamedAction(element.dataset.wmsCallSelf, [element], element);
            return;
        }

        if (element.dataset.wmsCalls) {
            event.preventDefault();
            try {
                JSON.parse(element.dataset.wmsCalls).forEach(function (call) {
                    if (!Array.isArray(call) || call.length === 0) return;
                    callNamedAction(call[0], call.slice(1), element);
                });
            } catch (error) {
                console.warn('Invalid WMS action sequence.', error);
            }
            return;
        }

        if (element.dataset.wmsCall) {
            event.preventDefault();
            callNamedAction(element.dataset.wmsCall, parseActionArgs(element), element);
        }
    }

    function enhanceDataActions() {
        document.addEventListener('click', function (event) {
            var element = event.target.closest('[data-wms-window-action], [data-wms-notify-title], [data-wms-click-target], [data-wms-export-table], [data-wms-close-modal], [data-wms-open-modal], [data-wms-call-self], [data-wms-calls], [data-wms-call]');
            if (!element) return;
            runDataAction(element, event);
        });

        document.addEventListener('change', function (event) {
            var element = event.target;
            if (!(element instanceof HTMLElement)) return;
            if (element.dataset.wmsSubmitForm === 'true' && element.form) element.form.submit();
            if (element.dataset.wmsRedirectRoute) {
                var param = element.dataset.wmsRedirectParam || element.name || 'value';
                window.location.href = element.dataset.wmsRedirectRoute + '?' + encodeURIComponent(param) + '=' + encodeURIComponent(element.value || '');
            }
            if (element.dataset.wmsClearValidity === 'true' && typeof element.setCustomValidity === 'function') element.setCustomValidity('');
            if (element.dataset.wmsChangeCall) {
                var changeArgs = element.dataset.wmsChangeSelf === 'true'
                    ? [element]
                    : element.dataset.wmsChangeValue === 'true'
                        ? [parseActionValue(element.value)]
                        : parseActionArgs(element);
                callNamedAction(element.dataset.wmsChangeCall, changeArgs, element);
            }
        });

        document.addEventListener('input', function (event) {
            var element = event.target;
            if (!(element instanceof HTMLElement)) return;
            if (element.dataset.wmsClearValidity === 'true' && typeof element.setCustomValidity === 'function') element.setCustomValidity('');
            if (element.dataset.wmsInputCall) callNamedAction(element.dataset.wmsInputCall, element.dataset.wmsInputSelf === 'true' ? [element] : parseActionArgs(element), element);
        });

        document.addEventListener('invalid', function (event) {
            var element = event.target;
            if (!(element instanceof HTMLElement)) return;
            if (element.dataset.wmsInvalidMessage && typeof element.setCustomValidity === 'function') {
                element.setCustomValidity(element.dataset.wmsInvalidMessage);
            }
        }, true);
    }

    ready(function () {
        document.body.classList.add('enterprise-ui-ready');
        enhanceTables();
        enhanceForms();
        enhanceStatusBadges();
        enhanceDataWidths();
        enhanceDataActions();
    });
})();

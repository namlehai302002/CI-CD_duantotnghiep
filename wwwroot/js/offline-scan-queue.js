(function () {
    'use strict';

    if (document.body?.dataset?.wmsOperational !== 'true') {
        window.wmsOfflineQueue = {
            flush: function () { return Promise.resolve(); },
            render: function () { return Promise.resolve(); },
            enqueueForm: function () { return Promise.resolve(); }
        };
        return;
    }

    var DB_NAME = 'wms-offline-scan-queue';
    var STORE_NAME = 'operations';
    var DB_VERSION = 1;
    var FALLBACK_KEY = 'wms_offline_scan_queue_v1';
    var WIDGET_HIDDEN_KEY = 'wms_offline_queue_hidden';
    var DRAFT_PREFIX = 'wms_scan_draft:';
    var SENT_TTL_MS = 24 * 60 * 60 * 1000;
    var FAILED_TTL_MS = 7 * 24 * 60 * 60 * 1000;
    var CONFLICT_TTL_MS = 14 * 24 * 60 * 60 * 1000;
    var MAX_ATTEMPTS = 8;
    var BACKOFF_MS = [0, 10000, 30000, 120000, 300000, 900000, 1800000, 3600000];
    var dbPromise = null;
    var flushRunning = false;
    var widgetInitialized = false;

    function nowIso() {
        return new Date().toISOString();
    }

    function createId() {
        if (window.crypto && typeof window.crypto.randomUUID === 'function') {
            return window.crypto.randomUUID();
        }
        return 'op-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 10);
    }

    function openDb() {
        if (!('indexedDB' in window)) return Promise.reject(new Error('IndexedDB unavailable'));
        if (dbPromise) return dbPromise;
        dbPromise = new Promise(function (resolve, reject) {
            var request = indexedDB.open(DB_NAME, DB_VERSION);
            request.onupgradeneeded = function () {
                var db = request.result;
                if (!db.objectStoreNames.contains(STORE_NAME)) {
                    db.createObjectStore(STORE_NAME, { keyPath: 'id' });
                }
            };
            request.onsuccess = function () { resolve(request.result); };
            request.onerror = function () { reject(request.error || new Error('IndexedDB open failed')); };
        });
        return dbPromise;
    }

    function fallbackAll() {
        try {
            return JSON.parse(localStorage.getItem(FALLBACK_KEY) || '[]');
        } catch {
            return [];
        }
    }

    function fallbackSave(rows) {
        localStorage.setItem(FALLBACK_KEY, JSON.stringify(rows));
    }

    function storePut(operation) {
        return openDb().then(function (db) {
            return new Promise(function (resolve, reject) {
                var tx = db.transaction(STORE_NAME, 'readwrite');
                tx.objectStore(STORE_NAME).put(operation);
                tx.oncomplete = resolve;
                tx.onerror = function () { reject(tx.error); };
            });
        }).catch(function () {
            var rows = fallbackAll().filter(function (row) { return row.id !== operation.id; });
            rows.push(operation);
            fallbackSave(rows);
        });
    }

    function storeDelete(id) {
        return openDb().then(function (db) {
            return new Promise(function (resolve, reject) {
                var tx = db.transaction(STORE_NAME, 'readwrite');
                tx.objectStore(STORE_NAME).delete(id);
                tx.oncomplete = resolve;
                tx.onerror = function () { reject(tx.error); };
            });
        }).catch(function () {
            fallbackSave(fallbackAll().filter(function (row) { return row.id !== id; }));
        });
    }

    function storeAll() {
        return openDb().then(function (db) {
            return new Promise(function (resolve, reject) {
                var tx = db.transaction(STORE_NAME, 'readonly');
                var request = tx.objectStore(STORE_NAME).getAll();
                request.onsuccess = function () { resolve(request.result || []); };
                request.onerror = function () { reject(request.error); };
            });
        }).catch(function () {
            return fallbackAll();
        });
    }

    function serializeForm(form, submitter) {
        var formData = new FormData(form);
        if (submitter && submitter.name && !formData.has(submitter.name)) {
            formData.append(submitter.name, submitter.value || '');
        }
        var fields = [];
        formData.forEach(function (value, key) {
            fields.push({ name: key, value: String(value) });
        });
        return fields;
    }

    function buildFormData(operation) {
        var data = new FormData();
        operation.fields.forEach(function (field) {
            data.append(field.name, field.value);
        });
        return data;
    }

    function operationSummary(form) {
        return form.getAttribute('data-offline-description')
            || form.getAttribute('aria-label')
            || form.action
            || 'Thao tác quét';
    }

    function operationType(form) {
        return form.getAttribute('data-offline-operation-type') || 'Thao tác quét';
    }

    function draftKey(form) {
        var action = form.getAttribute('action') || location.pathname;
        var identity = form.querySelector('input[name="id"], input[name="movementTaskId"], input[name="loadId"]');
        return DRAFT_PREFIX + action + ':' + (identity ? identity.value : form.id || operationSummary(form));
    }

    function saveDraft(form) {
        try {
            localStorage.setItem(draftKey(form), JSON.stringify({
                savedAt: nowIso(),
                fields: serializeForm(form).filter(function (field) { return field.name !== '__RequestVerificationToken'; })
            }));
        } catch { }
    }

    function clearDraft(form) {
        try {
            localStorage.removeItem(draftKey(form));
        } catch { }
    }

    function restoreDraft(form) {
        try {
            var raw = localStorage.getItem(draftKey(form));
            if (!raw) return;
            var draft = JSON.parse(raw);
            if (!draft || !Array.isArray(draft.fields)) return;
            draft.fields.forEach(function (field) {
                var target = form.querySelector('[name="' + cssEscape(field.name) + '"]');
                if (!target || target.type === 'hidden') return;
                target.value = field.value;
                target.dispatchEvent(new Event('change', { bubbles: true }));
            });
            showFormNotice(form, 'Đã khôi phục thao tác quét chưa gửi.');
        } catch { }
    }

    function showFormNotice(form, text) {
        var existing = form.querySelector('.offline-queue-form-notice');
        if (!existing) {
            existing = document.createElement('div');
            existing.className = 'offline-queue-form-notice';
            form.prepend(existing);
        }
        existing.textContent = text;
    }

    function notify(title, icon) {
        if (window.Swal) {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: icon || 'info',
                title: title,
                showConfirmButton: false,
                timer: 3600,
                timerProgressBar: true
            });
        }
    }

    function statusLabel(status) {
        switch (status) {
            case 'pending': return 'Chờ gửi';
            case 'sending': return 'Đang gửi';
            case 'sent': return 'Đã gửi';
            case 'failed': return 'Lỗi mạng';
            case 'blocked': return 'Cần xử lý';
            default: return status || 'Không rõ';
        }
    }

    function escapeHtml(value) {
        var div = document.createElement('div');
        div.textContent = value == null ? '' : String(value);
        return div.innerHTML;
    }

    function cssEscape(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
        return String(value).replace(/["\\]/g, '\\$&');
    }

    function isWidgetHidden() {
        try {
            return localStorage.getItem(WIDGET_HIDDEN_KEY) === 'true';
        } catch {
            return false;
        }
    }

    function setWidgetHidden(hidden) {
        var widget = document.getElementById('offlineQueueWidget');
        if (!widget) return;
        widget.classList.toggle('is-hidden', hidden);
        if (hidden) {
            document.getElementById('offlineQueuePanel')?.classList.remove('open');
        }
        try {
            localStorage.setItem(WIDGET_HIDDEN_KEY, hidden ? 'true' : 'false');
        } catch { }
    }

    function initWidget() {
        if (widgetInitialized) return;
        widgetInitialized = true;
        setWidgetHidden(isWidgetHidden());
        document.getElementById('offlineQueueToggle')?.addEventListener('click', function () {
            document.getElementById('offlineQueuePanel')?.classList.toggle('open');
            renderQueue();
        });
        document.getElementById('offlineQueueHide')?.addEventListener('click', function () {
            setWidgetHidden(true);
        });
        document.getElementById('offlineQueueRestore')?.addEventListener('click', function () {
            setWidgetHidden(false);
            renderQueue();
        });
        document.getElementById('offlineQueueClose')?.addEventListener('click', function () {
            document.getElementById('offlineQueuePanel')?.classList.remove('open');
        });
        document.getElementById('offlineQueueFlush')?.addEventListener('click', function () {
            flush();
        });
    }

    function calculateNextRetryAt(operation) {
        var attempts = Math.max(0, operation.attempts || 0);
        var delay = BACKOFF_MS[Math.min(attempts, BACKOFF_MS.length - 1)];
        var base = new Date(operation.updatedAt || operation.createdAt || nowIso()).getTime();
        return new Date(base + delay).toISOString();
    }

    function isDue(operation, force) {
        if (force) return true;
        if (operation.status === 'blocked' || operation.status === 'conflict' || operation.status === 'deadletter') return false;
        if ((operation.attempts || 0) >= MAX_ATTEMPTS) return false;
        if (!operation.nextRetryAt) return true;
        return new Date(operation.nextRetryAt).getTime() <= Date.now();
    }

    function renderQueue() {
        initWidget();
        return storeAll().then(function (rows) {
            rows.sort(function (a, b) { return (b.createdAt || '').localeCompare(a.createdAt || ''); });
            var active = rows.filter(function (row) { return row.status !== 'sent'; });
            var problemCount = active.filter(function (row) { return row.status === 'failed' || row.status === 'blocked' || row.status === 'conflict' || row.status === 'deadletter'; }).length;
            var retryingCount = active.filter(function (row) { return row.status === 'pending' || row.status === 'failed'; }).length;
            var count = active.length;
            var widget = document.getElementById('offlineQueueWidget');
            var badges = [
                document.getElementById('offlineQueueBadge'),
                document.getElementById('offlineQueueRestoreBadge')
            ].filter(Boolean);
            var summary = document.getElementById('offlineQueueSummary');
            var list = document.getElementById('offlineQueueList');

            if (widget) {
                widget.classList.toggle('is-empty', count === 0);
            }
            badges.forEach(function (badge) {
                badge.textContent = count > 99 ? '99+' : String(count);
                badge.classList.toggle('visible', count > 0);
                badge.classList.toggle('danger', problemCount > 0);
            });
            if (summary) {
                summary.textContent = count === 0
                    ? 'Không có thao tác chờ gửi.'
                    : count + ' thao tác chờ xử lý' + (problemCount ? ', ' + problemCount + ' thao tác lỗi.' : '.');
            }
            if (summary) {
                summary.dataset.pending = String(count);
                summary.dataset.retrying = String(retryingCount);
                summary.dataset.problems = String(problemCount);
            }
            if (!list) return;
            if (active.length === 0) {
                list.innerHTML = '<div class="offline-queue-empty">Các thao tác quét đã gửi xong.</div>';
                return;
            }
            list.innerHTML = active.map(function (row) {
                return '<div class="offline-queue-row status-' + escapeHtml(row.status) + '">' +
                    '<div class="offline-queue-row-main">' +
                    '<strong>' + escapeHtml(row.type) + '</strong>' +
                    '<span>' + escapeHtml(row.description) + '</span>' +
                    '<small>' + statusLabel(row.status) + ' · thử ' + (row.attempts || 0) + ' lần · ' + escapeHtml(formatTime(row.updatedAt || row.createdAt)) + '</small>' +
                    (row.error ? '<em>' + escapeHtml(row.error) + '</em>' : '') +
                    '</div>' +
                    '<div class="offline-queue-row-actions">' +
                    '<button type="button" class="btn btn-sm btn-primary" data-offline-retry="' + escapeHtml(row.id) + '">Gửi lại</button>' +
                    '<button type="button" class="btn btn-sm btn-secondary" data-offline-remove="' + escapeHtml(row.id) + '">Bỏ</button>' +
                    '</div>' +
                    '</div>';
            }).join('');
            list.querySelectorAll('[data-offline-retry]').forEach(function (button) {
                button.addEventListener('click', function () { flush(button.getAttribute('data-offline-retry')); });
            });
            list.querySelectorAll('[data-offline-remove]').forEach(function (button) {
                button.addEventListener('click', function () {
                    storeDelete(button.getAttribute('data-offline-remove')).then(renderQueue);
                });
            });
        });
    }

    function formatTime(value) {
        if (!value) return '';
        try {
            return new Date(value).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' });
        } catch {
            return value;
        }
    }

    function classifyFailure(response, payload) {
        if (response.status === 401 || response.status === 419) {
            return { status: 'blocked', error: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại rồi gửi lại thao tác.' };
        }
        if (response.status === 403) {
            return { status: 'blocked', error: payload?.message || 'Bạn không có quyền thực hiện thao tác này.' };
        }
        if ([400, 409, 422].includes(response.status)) {
            if (response.status === 409) {
                return { status: 'conflict', error: payload?.message || 'Thao tac offline xung dot voi du lieu moi tren may chu.', conflictKey: payload?.conflictKey || payload?.operationId || null };
            }
            return { status: 'blocked', error: payload?.message || 'Máy chủ từ chối thao tác vì chưa đạt điều kiện nghiệp vụ.' };
        }
        return { status: 'failed', error: payload?.message || 'Máy chủ chưa xử lý được thao tác. Vui lòng gửi lại.' };
    }

    function sendOperation(operation) {
        var formData = buildFormData(operation);
        return fetch(operation.url, {
            method: operation.method || 'POST',
            body: formData,
            credentials: 'same-origin',
            headers: {
                'X-WMS-Queued-Operation': 'true',
                'X-WMS-Offline-Operation-Id': operation.id,
                'X-WMS-Offline-Attempt': String(operation.attempts || 0),
                'X-Requested-With': 'XMLHttpRequest'
            },
            redirect: 'follow'
        }).then(function (response) {
            var contentType = response.headers.get('content-type') || '';
            if (!contentType.includes('application/json')) {
                if (!response.ok) {
                    throw { networkLike: false, status: 'blocked', error: 'Máy chủ không trả phản hồi hợp lệ. Có thể phiên đăng nhập đã hết hạn.' };
                }
                throw { networkLike: false, status: 'failed', error: 'Thao tác chưa nhận được phản hồi hàng đợi hợp lệ.' };
            }
            return response.json().then(function (payload) {
                if (response.ok && payload && payload.success !== false) {
                    return payload;
                }
                var failure = classifyFailure(response, payload);
                throw { networkLike: false, status: failure.status, error: failure.error, conflictKey: failure.conflictKey || null, payload: payload };
            });
        });
    }

    function flush(activeOperationId) {
        if (flushRunning && !activeOperationId) return Promise.resolve();
        flushRunning = true;
        return storeAll()
            .then(function (rows) {
                var due = rows.filter(function (row) {
                    if (activeOperationId && row.id !== activeOperationId) return false;
                    return (row.status === 'pending' || row.status === 'failed') && isDue(row, !!activeOperationId);
                }).sort(function (a, b) { return (a.createdAt || '').localeCompare(b.createdAt || ''); });

                var chain = Promise.resolve();
                due.forEach(function (operation) {
                    chain = chain.then(function () {
                        if (!navigator.onLine) return;
                        if ((operation.attempts || 0) >= MAX_ATTEMPTS) {
                            operation.status = 'deadletter';
                            operation.error = 'Thao tác đã gửi lại quá nhiều lần. Vui lòng kiểm tra thủ công.';
                            operation.updatedAt = nowIso();
                            operation.nextRetryAt = null;
                            return storePut(operation).then(renderQueue);
                        }
                        operation.status = 'sending';
                        operation.attempts = (operation.attempts || 0) + 1;
                        operation.updatedAt = nowIso();
                        operation.error = null;
                        operation.nextRetryAt = calculateNextRetryAt(operation);
                        return storePut(operation)
                            .then(renderQueue)
                            .then(function () { return sendOperation(operation); })
                            .then(function (payload) {
                                operation.status = 'sent';
                                operation.updatedAt = nowIso();
                                operation.response = payload;
                                operation.error = null;
                                operation.nextRetryAt = null;
                                operation.conflictKey = null;
                                return storePut(operation).then(function () {
                                    clearDraftByKey(operation.draftKey);
                                    notify(operation.description + ' đã gửi thành công.', 'success');
                                    if (activeOperationId === operation.id && payload.redirectUrl) {
                                        window.location.href = payload.redirectUrl;
                                    }
                                });
                            })
                            .catch(function (error) {
                                operation.status = error && error.status ? error.status : 'failed';
                                operation.error = error && error.error ? error.error : 'Mất kết nối hoặc máy chủ chưa phản hồi.';
                                operation.conflictKey = error ? (error.conflictKey || (error.payload ? (error.payload.conflictKey || error.payload.operationId || null) : null)) : null;
                                if (operation.status === 'failed') {
                                    operation.nextRetryAt = calculateNextRetryAt(operation);
                                }
                                if (operation.status === 'blocked' || operation.status === 'conflict') {
                                    operation.nextRetryAt = null;
                                }
                                operation.updatedAt = nowIso();
                                return storePut(operation).then(function () {
                                    notify(operation.error, operation.status === 'blocked' || operation.status === 'conflict' ? 'warning' : 'error');
                                });
                            })
                            .then(renderQueue);
                    });
                });
                return chain;
            })
            .then(cleanup)
            .finally(function () {
                flushRunning = false;
                renderQueue();
            });
    }

    function clearDraftByKey(key) {
        if (!key) return;
        try {
            localStorage.removeItem(key);
        } catch { }
    }

    function cleanup() {
        var now = Date.now();
        return storeAll().then(function (rows) {
            var removals = rows.filter(function (row) {
                var updated = new Date(row.updatedAt || row.createdAt || 0).getTime();
                if (!updated) return false;
                if (row.status === 'sent') return now - updated > SENT_TTL_MS;
                if (row.status === 'failed' || row.status === 'blocked') return now - updated > FAILED_TTL_MS;
                if (row.status === 'conflict' || row.status === 'deadletter') return now - updated > CONFLICT_TTL_MS;
                return false;
            });
            return Promise.all(removals.map(function (row) { return storeDelete(row.id); }));
        });
    }

    function enqueueForm(form, submitter) {
        var id = createId();
        var key = draftKey(form);
        var operation = {
            id: id,
            url: form.action,
            method: (form.method || 'POST').toUpperCase(),
            type: operationType(form),
            description: operationSummary(form),
            fields: serializeForm(form, submitter),
            status: 'pending',
            attempts: 0,
            createdAt: nowIso(),
            updatedAt: nowIso(),
            nextRetryAt: null,
            conflictKey: null,
            draftKey: key,
            pageUrl: location.href
        };
        return storePut(operation).then(function () {
            renderQueue();
            notify(navigator.onLine ? 'Đã đưa thao tác vào hàng đợi và đang gửi.' : 'Đã lưu thao tác. Hệ thống sẽ gửi khi có mạng.', navigator.onLine ? 'info' : 'warning');
            if (navigator.onLine) return flush(operation.id);
            return undefined;
        });
    }

    function bindForm(form) {
        if (form.dataset.offlineQueueBound === 'true') return;
        form.dataset.offlineQueueBound = 'true';
        restoreDraft(form);
        form.addEventListener('input', function () { saveDraft(form); }, true);
        form.addEventListener('change', function () { saveDraft(form); }, true);
        form.addEventListener('submit', function (event) {
            event.preventDefault();
            var submitter = event.submitter || document.activeElement;
            saveDraft(form);
            enqueueForm(form, submitter).then(function () {
                clearDraft(form);
            });
        });
    }

    function init() {
        initWidget();
        document.querySelectorAll('form[data-offline-queue="true"]').forEach(bindForm);
        renderQueue().then(function () {
            if (navigator.onLine) flush();
        });
    }

    window.addEventListener('online', function () { flush(); });
    document.addEventListener('DOMContentLoaded', init);

    window.wmsOfflineQueue = {
        flush: flush,
        render: renderQueue,
        enqueueForm: enqueueForm,
        exportQueueSnapshot: function () {
            return storeAll().then(function (rows) {
                return rows.map(function (row) {
                    return {
                        id: row.id,
                        type: row.type,
                        status: row.status,
                        attempts: row.attempts || 0,
                        nextRetryAt: row.nextRetryAt || null,
                        conflictKey: row.conflictKey || null,
                        createdAt: row.createdAt,
                        updatedAt: row.updatedAt
                    };
                });
            });
        }
    };
})();

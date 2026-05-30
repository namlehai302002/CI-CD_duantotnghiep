(function () {
    'use strict';

    var state = {
        instance: null,
        callback: null,
        options: {},
        lock: false,
        attempts: 0
    };

    var defaultOptions = {
        closeOnSuccess: false,
        append: false,
        separator: '\n',
        submitForm: false,
        lockMs: 900
    };

    function mergeOptions(options) {
        var merged = {};
        Object.keys(defaultOptions).forEach(function (key) {
            merged[key] = defaultOptions[key];
        });
        options = options || {};
        Object.keys(options).forEach(function (key) {
            merged[key] = options[key];
        });
        return merged;
    }

    function getElement(id) {
        return document.getElementById(id);
    }

    function setText(id, text, kind) {
        var element = getElement(id);
        if (!element) return;
        element.textContent = text || '';
        element.classList.remove('success', 'danger', 'warning');
        if (kind) element.classList.add(kind);
        element.style.display = text ? 'block' : 'none';
    }

    function setHtml(id, html, kind) {
        var element = getElement(id);
        if (!element) return;
        element.innerHTML = html || '';
        element.classList.remove('success', 'danger', 'warning');
        if (kind) element.classList.add(kind);
        element.style.display = html ? 'block' : 'none';
    }

    function playFeedback(success) {
        try {
            var ctx = new (window.AudioContext || window.webkitAudioContext)();
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.type = 'sine';
            osc.frequency.value = success ? 880 : 330;
            gain.gain.value = 0.15;
            osc.start();
            osc.stop(ctx.currentTime + (success ? 0.12 : 0.35));
        } catch (e) {
            // Thiết bị có thể chặn âm thanh khi chưa có tương tác người dùng.
        }

        if (navigator.vibrate) {
            try {
                navigator.vibrate(success ? 60 : [80, 40, 80]);
            } catch (e) {
                // Không phải trình duyệt nào cũng cho rung.
            }
        }
    }

    function supportedFormats() {
        if (!window.Html5QrcodeSupportedFormats) return undefined;
        var formats = window.Html5QrcodeSupportedFormats;
        return [
            formats.EAN_13,
            formats.EAN_8,
            formats.CODE_128,
            formats.CODE_39,
            formats.UPC_A,
            formats.UPC_E,
            formats.QR_CODE,
            formats.DATA_MATRIX,
            formats.ITF,
            formats.CODABAR
        ].filter(Boolean);
    }

    function isSecureCameraContext() {
        var host = window.location.hostname;
        return window.location.protocol === 'https:'
            || host === ['local', 'host'].join('')
            || host === '::1'
            || /^127(?:\.\d{1,3}){3}$/.test(host);
    }

    function markReaderSuccess() {
        var reader = getElement('reader');
        if (!reader) return;
        reader.classList.add('scanner-reader-success');
        window.setTimeout(function () {
            reader.classList.remove('scanner-reader-success');
        }, 650);
    }

    function resolveTarget(target) {
        if (!target) return null;
        if (typeof target !== 'string') return target;
        return document.getElementById(target) || document.querySelector(target);
    }

    function fillField(target, value, options) {
        var field = resolveTarget(target);
        if (!field) return false;
        var opts = mergeOptions(options);
        var current = (field.value || '').trim();

        if (opts.append) {
            field.value = current ? current + opts.separator + value : value;
        } else {
            field.value = value;
        }

        field.dispatchEvent(new Event('input', { bubbles: true }));
        field.dispatchEvent(new Event('change', { bubbles: true }));
        field.focus();

        if (!opts.append && typeof field.select === 'function') {
            field.select();
        }

        if (opts.submitForm) {
            window.setTimeout(function () {
                var form = field.closest('form');
                if (!form) return;
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit();
                } else {
                    form.submit();
                }
            }, 80);
        }

        return true;
    }

    function scanToField(target, options) {
        var opts = mergeOptions(options);
        openScannerModal(function (decodedText) {
            fillField(target, decodedText, opts);
            playFeedback(true);
        }, opts);
    }

    function resetModal() {
        var manualInput = getElement('manualBarcodeInput');
        if (manualInput) manualInput.value = '';
        setText('scanStatus', '', '');
        setText('scanDebug', '', '');
        var reader = getElement('reader');
        if (reader) reader.classList.remove('scanner-reader-success');
    }

    function startCamera() {
        if (!window.Html5Qrcode) {
            setText('scanStatus', 'Không tải được thư viện quét mã. Vui lòng tải lại trang hoặc nhập mã thủ công.', 'danger');
            return;
        }

        if (state.instance) return;

        if (!isSecureCameraContext()) {
            setHtml('scanStatus', '<strong>Cần kết nối bảo mật</strong> để dùng camera trên điện thoại. Vui lòng mở hệ thống bằng địa chỉ HTTPS.', 'warning');
        }

        state.instance = new window.Html5Qrcode('reader', {
            formatsToSupport: supportedFormats(),
            verbose: false
        });

        var config = {
            fps: 10,
            qrbox: function (viewfinderWidth, viewfinderHeight) {
                var width = Math.min(viewfinderWidth * 0.9, 420);
                var height = Math.min(viewfinderHeight * 0.52, 220);
                return { width: Math.floor(width), height: Math.floor(height) };
            },
            experimentalFeatures: { useBarCodeDetectorIfSupported: false },
            rememberLastUsedCamera: true,
            showTorchButtonIfSupported: true,
            disableFlip: false
        };

        state.instance.start(
            { facingMode: 'environment' },
            config,
            handleDecoded,
            handleScanMiss
        ).then(function () {
            setText('scanDebug', 'Camera đang hoạt động. Giữ điện thoại cách mã khoảng 10-20 cm.', '');
        }).catch(function (error) {
            state.instance = null;
            setHtml('scanStatus', cameraErrorMessage(error), 'danger');
        });
    }

    function cameraErrorMessage(error) {
        var text = String(error || '');
        if (text.indexOf('NotAllowedError') >= 0) {
            return 'Bạn đã chặn quyền camera. Hãy mở quyền camera trong cài đặt trình duyệt rồi thử lại.';
        }
        if (text.indexOf('NotFoundError') >= 0) {
            return 'Không tìm thấy camera trên thiết bị này. Bạn vẫn có thể nhập mã thủ công.';
        }
        if (text.indexOf('NotReadableError') >= 0) {
            return 'Camera đang được ứng dụng khác sử dụng. Hãy đóng ứng dụng đó rồi thử lại.';
        }
        if (!isSecureCameraContext()) {
            return 'Trình duyệt yêu cầu HTTPS để mở camera trên điện thoại.';
        }
        return 'Không mở được camera. Vui lòng thử lại hoặc nhập mã thủ công.';
    }

    function handleDecoded(decodedText, decodedResult) {
        if (state.lock) return;
        state.lock = true;

        var decoded = (decodedText || '').trim();
        if (!decoded) {
            state.lock = false;
            return;
        }

        var formatName = decodedResult && decodedResult.result && decodedResult.result.format
            ? decodedResult.result.format.formatName
            : 'mã quét';

        markReaderSuccess();
        playFeedback(true);
        setText('scanStatus', 'Đã quét: ' + decoded + ' (' + formatName + ')', 'success');

        Promise.resolve(state.callback ? state.callback(decoded, decodedResult) : null)
            .then(function () {
                if (state.options.closeOnSuccess) {
                    closeScannerModal();
                    return;
                }
                window.setTimeout(function () {
                    state.lock = false;
                }, state.options.lockMs || defaultOptions.lockMs);
            })
            .catch(function () {
                playFeedback(false);
                setText('scanStatus', 'Mã đã quét nhưng thao tác xử lý bị lỗi. Vui lòng thử lại.', 'danger');
                state.lock = false;
            });
    }

    function handleScanMiss() {
        state.attempts += 1;
        if (state.attempts % 35 !== 0) return;
        setText('scanDebug', 'Đang tìm mã. Hãy giữ mã nằm gọn trong khung và tránh phản sáng.', '');
    }

    window.openScannerModal = function (onSuccess, options) {
        var modal = getElement('scannerModal');
        if (!modal) return;
        state.callback = typeof onSuccess === 'function' ? onSuccess : null;
        state.options = mergeOptions(options);
        state.lock = false;
        state.attempts = 0;
        resetModal();
        modal.classList.add('active');
        modal.setAttribute('aria-hidden', 'false');
        startCamera();
    };

    window.closeScannerModal = function () {
        var modal = getElement('scannerModal');
        if (modal) {
            modal.classList.remove('active');
            modal.setAttribute('aria-hidden', 'true');
        }
        state.lock = false;
        if (!state.instance) return;
        state.instance.stop()
            .then(function () {
                state.instance.clear();
                state.instance = null;
            })
            .catch(function () {
                state.instance = null;
            });
    };

    window.submitManualBarcode = function () {
        var input = getElement('manualBarcodeInput');
        var code = (input && input.value ? input.value : '').trim();
        if (!code) return;
        if (input) input.value = '';
        handleDecoded(code, null);
    };

    window.toggleWebcamScanner = function (onSuccess, options) {
        window.openScannerModal(onSuccess, options);
    };

    function parseAdvancedBarcode(rawValue) {
        var raw = (rawValue || '').trim();
        if (!raw) return { success: false, kind: 'unknown', errorMessage: 'Mã quét đang trống.' };
        if (/^(PLT|PALLET):/i.test(raw)) return { success: true, kind: 'pallet', palletCode: raw.split(':').slice(1).join(':').trim().toUpperCase(), rawValue: raw };
        if (/^(SER|S):/i.test(raw)) return { success: true, kind: 'serial', serialNumber: raw.split(':').slice(1).join(':').trim().toUpperCase(), rawValue: raw };
        if (/^(EPC|RFID):/i.test(raw)) return { success: true, kind: 'rfid', rfidEpc: raw.split(':').slice(1).join(':').trim().toUpperCase(), rawValue: raw };

        var value = raw.indexOf(']C1') === 0 ? raw.substring(3) : raw;
        value = value.replace(/[()]/g, '');
        if (value.indexOf('01') === 0 || value.indexOf('00') === 0) {
            return parseGs1(value, raw);
        }
        return { success: true, kind: 'plain', rawValue: raw };
    }

    function parseGs1(value, raw) {
        var result = { success: true, kind: 'gs1', rawValue: raw };
        var index = 0;
        var gs = String.fromCharCode(29);
        function readFixed(ai, len, prop) {
            if (value.substring(index, index + ai.length) !== ai) return false;
            var start = index + ai.length;
            if (start + len > value.length) throw new Error('GS1 AI ' + ai + ' thiếu dữ liệu.');
            result[prop] = value.substring(start, start + len);
            index = start + len;
            if (value[index] === gs) index++;
            return true;
        }
        function readVariable(ai, max, prop) {
            if (value.substring(index, index + ai.length) !== ai) return false;
            var start = index + ai.length;
            var end = value.indexOf(gs, start);
            if (end < 0) end = Math.min(value.length, start + max);
            result[prop] = value.substring(start, end);
            index = end;
            if (value[index] === gs) index++;
            return true;
        }
        try {
            while (index < value.length) {
                if (readFixed('01', 14, 'gtin')) continue;
                if (readFixed('00', 18, 'sscc')) continue;
                if (readFixed('17', 6, 'expiryYyMmDd')) continue;
                if (readVariable('10', 20, 'lotNumber')) continue;
                if (readVariable('21', 20, 'serialNumber')) continue;
                if (readVariable('37', 8, 'quantity')) continue;
                return { success: false, kind: 'unknown', rawValue: raw, errorMessage: 'Không đọc được mã GS1 tại vị trí ' + (index + 1) + '.' };
            }
        } catch (error) {
            return { success: false, kind: 'unknown', rawValue: raw, errorMessage: error.message };
        }
        if (!result.gtin && !result.sscc) return { success: false, kind: 'unknown', rawValue: raw, errorMessage: 'Mã GS1 thiếu GTIN hoặc SSCC.' };
        return result;
    }

    function parseBulkBarcodes(rawValues) {
        return (rawValues || '').split(/[\r\n,;]+/).map(function (x) { return x.trim(); }).filter(Boolean).map(parseAdvancedBarcode);
    }

    window.wmsScanner = {
        open: window.openScannerModal,
        close: window.closeScannerModal,
        submitManual: window.submitManualBarcode,
        fillField: fillField,
        scanToField: scanToField,
        playFeedback: playFeedback,
        parseBarcode: parseAdvancedBarcode,
        parseBulk: parseBulkBarcodes
    };

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && getElement('scannerModal')?.classList.contains('active')) {
            window.closeScannerModal();
        }
    });
})();

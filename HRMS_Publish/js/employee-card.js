(function () {
    'use strict';

    var cfg = window.hrmsEmployeeCard || {};
    var canvas = document.getElementById('qrCanvas');
    var printArea = document.getElementById('idCardPrintArea');
    var cardPhoto = document.getElementById('cardEmployeePhoto');
    var defaultAvatar = cfg.defaultAvatarUrl || '/images/default-avatar.svg';

    function showDefaultAvatar(slot) {
        if (!slot) return;
        slot.innerHTML = '';
        var img = document.createElement('img');
        img.src = defaultAvatar;
        img.alt = 'No profile photo';
        img.className = 'id-card__photo id-card__photo--default';
        slot.appendChild(img);
    }

    if (cardPhoto) {
        cardPhoto.addEventListener('error', function () {
            showDefaultAvatar(document.getElementById('employeePhotoSlot'));
        });
    }

    if (canvas && cfg.qrPayload && window.QRCode) {
        QRCode.toCanvas(canvas, cfg.qrPayload, {
            width: 72,
            margin: 1,
            color: { dark: '#1a1f36', light: '#ffffff' }
        }, function () { /* ignore errors */ });
    }

    var btnPrint = document.getElementById('btnPrintCard');
    if (btnPrint) {
        btnPrint.addEventListener('click', function () {
            window.print();
        });
    }

    var btnPdf = document.getElementById('btnDownloadPdf');
    if (btnPdf && printArea && window.html2canvas && window.jspdf) {
        btnPdf.addEventListener('click', function () {
            btnPdf.disabled = true;
            btnPdf.textContent = 'Generating…';

            html2canvas(printArea, {
                scale: 3,
                useCORS: true,
                backgroundColor: '#ffffff',
                logging: false
            }).then(function (canvasEl) {
                var imgData = canvasEl.toDataURL('image/png');
                var pdf = new window.jspdf.jsPDF({
                    orientation: canvasEl.width >= canvasEl.height ? 'landscape' : 'portrait',
                    unit: 'px',
                    format: [canvasEl.width, canvasEl.height]
                });
                pdf.addImage(imgData, 'PNG', 0, 0, canvasEl.width, canvasEl.height);
                var code = (cfg.qrPayload || 'employee').replace(/[^\w\-]+/g, '_');
                pdf.save('ID_Card_' + code + '.pdf');
            }).catch(function () {
                alert('Could not generate PDF. Try using Print and Save as PDF instead.');
            }).finally(function () {
                btnPdf.disabled = false;
                btnPdf.textContent = 'Download PDF';
            });
        });
    }
})();

// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function submitForm() {
    var dayReport = document.getElementById("dayReportTime").value;
    if (dayReport.length < 1) { 
        showAlert("Vui lòng chọn ngày báo cáo", "warning");
        return;
    }
    
    var fileTCInput = document.getElementById("fileFileTC");
    if (fileTCInput.files.length < 1) {
        showAlert("Vui lòng chọn file TC (File nhân viên)", "warning");
        return;
    }
 
    var fileReportCDRInput = document.getElementById("fileFileReport");
    if (fileReportCDRInput.files.length < 1) {
        showAlert("Vui lòng chọn file báo cáo (File CDR)", "warning");
        return;
    }
    
    // Show loading overlay
    showLoadingOverlay("Đang xử lý file...", "Vui lòng chờ kết quả");
    
    // Disable submit button
    var submitBtn = document.querySelector('button[onclick="submitForm()"]');
    if (submitBtn) {
        submitBtn.classList.add('btn-loading');
        submitBtn.disabled = true;
    }
    
    // Submit form after a short delay
    setTimeout(function() {
        document.getElementById('formSubmitReport').submit();
    }, 1000);
}

function showLoadingOverlay(title, message) {
    var loadingDiv = document.getElementById('showLoadding');
    if (loadingDiv) {
        var titleElement = loadingDiv.querySelector('.loading-title');
        var messageElement = loadingDiv.querySelector('.loading-message');
        
        if (titleElement) titleElement.textContent = title;
        if (messageElement) messageElement.textContent = message;
        
        loadingDiv.style.display = "flex";
        
        // Add fade-in animation
        setTimeout(function() {
            loadingDiv.style.opacity = "1";
        }, 10);
    }
}

function hideLoadingOverlay() {
    var loadingDiv = document.getElementById('showLoadding');
    if (loadingDiv) {
        loadingDiv.style.opacity = "0";
        setTimeout(function() {
            loadingDiv.style.display = "none";
        }, 300);
    }
}

function showAlert(message, type = "info", details = null) {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.alert-toast');
    existingAlerts.forEach(alert => alert.remove());

    // Create alert element
    var alertDiv = document.createElement('div');
    alertDiv.className = `alert-toast alert-${type}`;
    
    // Set icon based on type
    let icon = 'fas fa-info-circle';
    if (type === 'success') icon = 'fas fa-check-circle';
    else if (type === 'danger' || type === 'error') icon = 'fas fa-exclamation-circle';
    else if (type === 'warning') icon = 'fas fa-exclamation-triangle';
    else if (type === 'system_error') icon = 'fas fa-bug';
    
    // Create details section if provided
    let detailsHtml = '';
    if (details) {
        detailsHtml = `
            <div class="alert-details">
                <small>${details}</small>
            </div>
        `;
    }
    
    alertDiv.innerHTML = `
        <div class="alert-content">
            <i class="${icon}"></i>
            <div class="alert-text">
                <div class="alert-message">${message}</div>
                ${detailsHtml}
            </div>
        </div>
        <button class="alert-close" onclick="this.parentElement.remove()">
            <i class="fas fa-times"></i>
        </button>
    `;

    // Add to page
    document.body.appendChild(alertDiv);

    // Auto remove after 8 seconds for errors, 5 seconds for others
    const timeout = (type === 'danger' || type === 'error' || type === 'system_error') ? 8000 : 5000;
    setTimeout(function() {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, timeout);
}

// Global function for other buttons
function showLoading() {
    showLoadingOverlay("Đang xử lý...", "Vui lòng chờ kết quả");
}

// AJAX functions for buttons
async function sendEmail() {
    showLoadingOverlay("Đang gửi email...", "Vui lòng chờ kết quả");
    
    try {
        const response = await fetch('/Home/SendEmail', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const result = await response.json();
        hideLoadingOverlay();
        
        if (result.success) {
            showAlert(result.message, "success", result.details);
        } else {
            showAlert(result.message, result.messageType || "danger", result.details);
        }
    } catch (error) {
        hideLoadingOverlay();
        showAlert("Lỗi kết nối: " + error.message, "danger");
    }
}

async function sendLatestPaymentEmail() {
    showLoadingOverlay("Đang gửi email payment...", "Vui lòng chờ kết quả");
    
    try {
        const response = await fetch('/Home/SendLatestPaymentEmail', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const result = await response.json();
        hideLoadingOverlay();
        
        if (result.success) {
            showAlert(result.message, "success", result.details);
        } else {
            showAlert(result.message, result.messageType || "danger", result.details);
        }
    } catch (error) {
        hideLoadingOverlay();
        showAlert("Lỗi kết nối: " + error.message, "danger");
    }
}

async function downloadPayment() {
    showLoadingOverlay("Đang tải file payment...", "Vui lòng chờ kết quả");
    
    try {
        const response = await fetch('/Home/DownloadPayment', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const result = await response.json();
        hideLoadingOverlay();
        
        if (result.success) {
            showAlert(result.message, "success", result.details);
        } else {
            showAlert(result.message, result.messageType || "danger", result.details);
        }
    } catch (error) {
        hideLoadingOverlay();
        showAlert("Lỗi kết nối: " + error.message, "danger");
    }
}

async function uploadToSFTP() {
    showLoadingOverlay("Đang upload lên SFTP...", "Vui lòng chờ kết quả");
    
    try {
        const response = await fetch('/Home/UploadToSFTP', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const result = await response.json();
        hideLoadingOverlay();
        
        if (result.success) {
            showAlert(result.message, "success", result.details);
        } else {
            showAlert(result.message, result.messageType || "danger", result.details);
        }
    } catch (error) {
        hideLoadingOverlay();
        showAlert("Lỗi kết nối: " + error.message, "danger");
    }
}
    
}
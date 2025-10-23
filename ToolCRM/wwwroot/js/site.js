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

function showAlert(message, type = "info") {
    // Create alert element
    var alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    alertDiv.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 10000;
        min-width: 300px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    `;
    
    alertDiv.innerHTML = `
        <i class="fas fa-${type === 'warning' ? 'exclamation-triangle' : 'info-circle'} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(alertDiv);
    
    // Auto remove after 5 seconds
    setTimeout(function() {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
}

// Global function for other buttons
function showLoading() {
    showLoadingOverlay("Đang xử lý...", "Vui lòng chờ kết quả");
}
    
}
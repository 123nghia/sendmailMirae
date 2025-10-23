// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function submitForm() {

    var dayReport = document.getElementById("dayReportTime").value;
    if (dayReport.length < 1) { 

        alert("chưa chọn ngày báo cáo");
        return;
    }
    var fileTCInput = document.getElementById("fileFileTC");
    if (fileTCInput.files.length < 1) {

        alert("chưa upload file TC");
        return;
    }
 
    var fileReportCDRInput = document.getElementById("fileFileReport");
    if (fileReportCDRInput.files.length < 1) {

        alert("chưa upload file báo cáo");
        return;
    }
    document.getElementById('contentdiv').style.display = "none";
    document.getElementById('showLoadding').style.display = "block";
    const myTimeout = setTimeout(myStopFunction, 2000);
    function myStopFunction() {
        document.getElementById('formSubmitReport').submit();
    }
    
}
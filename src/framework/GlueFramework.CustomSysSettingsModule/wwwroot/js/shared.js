window.showOrchardErrorMessage = function (message) {
    const notice =
        $(`<div class="alert alert-dismissible alert-danger fade show message-error" role="alert">
          ${message}
          <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
          </div>`).prependTo($(".ta-content"));
}
window.showOrchardInfoMessage = function (message) {
    const notice =
        $(`<div class="alert alert-dismissible alert-primary fade show message-info" role="alert">
          ${message}
          <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
          </div>`).prependTo($(".ta-content"));
}
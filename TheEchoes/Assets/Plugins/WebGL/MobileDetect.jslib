mergeInto(LibraryManager.library, {
  IsMobile: function () {
    var ua = navigator.userAgent || navigator.vendor || window.opera || "";
    var isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Windows Phone|Opera Mini/i.test(ua);
    if (!isMobile) {
      var touch = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
      var shortSide = Math.min(window.innerWidth, window.innerHeight);
      if (touch && shortSide <= 900) isMobile = true;
    }
    return isMobile ? 1 : 0;
  }
});

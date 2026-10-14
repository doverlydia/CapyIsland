// The browser's clipboard for the save code (UI/Screens/SaveCodeScreen). Unity's own copy buffer
// stays inside the page on WebGL, so copying and pasting go through the browser here.
mergeInto(LibraryManager.library, {
  CapyCopyText: function (textPtr) {
    var text = UTF8ToString(textPtr);
    function fallback() {
      var area = document.createElement("textarea");
      area.value = text;
      area.setAttribute("readonly", "");
      area.style.position = "fixed";
      area.style.opacity = "0";
      document.body.appendChild(area);
      area.select();
      try { document.execCommand("copy"); } catch (e) {}
      document.body.removeChild(area);
    }
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).catch(fallback);
    } else {
      fallback();
    }
  },

  CapyPasteText: function (objectPtr, methodPtr) {
    var target = UTF8ToString(objectPtr);
    var method = UTF8ToString(methodPtr);
    if (!navigator.clipboard || !navigator.clipboard.readText) {
      SendMessage(target, method, "");
      return;
    }
    navigator.clipboard.readText()
      .then(function (text) { SendMessage(target, method, text || ""); })
      .catch(function () { SendMessage(target, method, ""); });
  }
});

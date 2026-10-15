// Test builds only (see DevCommands.cs): gives the page capyDev("Type.Method args"), so a second
// copy of the game can be driven from the browser console while testing two-player features.
mergeInto(LibraryManager.library, {
  CapyDevHook: function (receiverPtr) {
    var receiver = UTF8ToString(receiverPtr);
    window.capyDev = function (command) { SendMessage(receiver, 'Run', command); };
  }
});

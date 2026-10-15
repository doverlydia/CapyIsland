// Sign in with Google in the browser game, for GoogleLogin.cs.
// Google only lets its own button start a sign-in, so a small card with that button is shown
// over the game. The ID token, or "cancel"/"offline", goes back to C# with SendMessage.
mergeInto(LibraryManager.library, {
  CapyGoogleLogin: function (clientIdPtr, receiverPtr, cancelPtr, localePtr) {
    var clientId = UTF8ToString(clientIdPtr);
    var receiver = UTF8ToString(receiverPtr);
    var cancelText = UTF8ToString(cancelPtr);
    var locale = UTF8ToString(localePtr);
    var veil;

    function close() { if (veil && veil.parentNode) veil.parentNode.removeChild(veil); }
    function answer(method, value) { close(); SendMessage(receiver, method, value); }

    function show() {
      var old = document.getElementById('capy-google');
      if (old) old.parentNode.removeChild(old);
      veil = document.createElement('div');
      veil.id = 'capy-google';
      veil.style.cssText = 'position:fixed;left:0;top:0;right:0;bottom:0;z-index:2147483647;display:flex;' +
        'align-items:center;justify-content:center;background:rgba(48,32,20,0.45);';
      var card = document.createElement('div');
      card.style.cssText = 'background:#FFF6E6;border-radius:22px;padding:26px 24px 14px;display:flex;' +
        'flex-direction:column;align-items:center;gap:12px;box-shadow:0 10px 30px rgba(0,0,0,0.25);';
      var holder = document.createElement('div');
      holder.style.cssText = 'min-height:44px;';
      var cancel = document.createElement('button');
      cancel.textContent = cancelText;
      cancel.style.cssText = 'border:none;background:none;color:#7A5A3C;font:600 16px sans-serif;padding:8px 18px;cursor:pointer;';
      cancel.onclick = function () { answer('OnGoogleError', 'cancel'); };
      veil.onclick = function (e) { if (e.target === veil) answer('OnGoogleError', 'cancel'); };
      card.appendChild(holder);
      card.appendChild(cancel);
      veil.appendChild(card);
      document.body.appendChild(veil);

      google.accounts.id.initialize({
        client_id: clientId,
        callback: function (r) { answer('OnGoogleToken', r.credential); },
        ux_mode: 'popup',
        auto_select: false,
        itp_support: true
      });
      google.accounts.id.renderButton(holder, {
        type: 'standard', theme: 'outline', size: 'large', shape: 'pill',
        text: 'continue_with', locale: locale, width: 260
      });
    }

    if (window.google && google.accounts && google.accounts.id) { show(); return; }
    var script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.onload = show;
    script.onerror = function () { SendMessage(receiver, 'OnGoogleError', 'offline'); };
    document.head.appendChild(script);
  }
});

mergeInto(LibraryManager.library, {
  TgIsAvailable: function () {
    try {
      return (window.Telegram && window.Telegram.WebApp) ? 1 : 0;
    } catch (e) {
      return 0;
    }
  },

  TgGetInitData: function () {
    try {
      const data = (window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initData)
        ? window.Telegram.WebApp.initData
        : "";

      const bufferSize = lengthBytesUTF8(data) + 1;
      const buffer = _malloc(bufferSize);
      stringToUTF8(data, buffer, bufferSize);
      return buffer;
    } catch (e) {
      const buffer = _malloc(1);
      stringToUTF8("", buffer, 1);
      return buffer;
    }
  },

  TgGetUserJson: function () {
    try {
      let user = null;

      if (window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initDataUnsafe) {
        user = window.Telegram.WebApp.initDataUnsafe.user || null;
      }

      const json = user ? JSON.stringify(user) : "";
      const bufferSize = lengthBytesUTF8(json) + 1;
      const buffer = _malloc(bufferSize);
      stringToUTF8(json, buffer, bufferSize);
      return buffer;
    } catch (e) {
      const buffer = _malloc(1);
      stringToUTF8("", buffer, 1);
      return buffer;
    }
  },

  TgGetStartParam: function () {
    try {
      let value = "";

      if (window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initDataUnsafe) {
        value = window.Telegram.WebApp.initDataUnsafe.start_param || "";
      }

      const bufferSize = lengthBytesUTF8(value) + 1;
      const buffer = _malloc(bufferSize);
      stringToUTF8(value, buffer, bufferSize);
      return buffer;
    } catch (e) {
      const buffer = _malloc(1);
      stringToUTF8("", buffer, 1);
      return buffer;
    }
  },

  TgGetPlatform: function () {
    try {
      const value = (window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.platform)
        ? window.Telegram.WebApp.platform
        : "";

      const bufferSize = lengthBytesUTF8(value) + 1;
      const buffer = _malloc(bufferSize);
      stringToUTF8(value, buffer, bufferSize);
      return buffer;
    } catch (e) {
      const buffer = _malloc(1);
      stringToUTF8("", buffer, 1);
      return buffer;
    }
  },

  TgGetVersion: function () {
    try {
      const value = (window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.version)
        ? window.Telegram.WebApp.version
        : "";

      const bufferSize = lengthBytesUTF8(value) + 1;
      const buffer = _malloc(bufferSize);
      stringToUTF8(value, buffer, bufferSize);
      return buffer;
    } catch (e) {
      const buffer = _malloc(1);
      stringToUTF8("", buffer, 1);
      return buffer;
    }
  },

  TgReadyAndExpand: function () {
    try {
      if (window.Telegram && window.Telegram.WebApp) {
        window.Telegram.WebApp.ready();
        window.Telegram.WebApp.expand();
      }
    } catch (e) {
      console.error(e);
    }
  },

  TgOpenInvoice: function (urlPtr, gameObjectPtr, callbackMethodPtr) {
    try {
      const url = UTF8ToString(urlPtr);
      const gameObjectName = UTF8ToString(gameObjectPtr);
      const callbackMethodName = UTF8ToString(callbackMethodPtr);

      if (!window.Telegram || !window.Telegram.WebApp) {
        console.error("Telegram WebApp API not found");

        if (typeof unityInstance !== "undefined" && unityInstance != null) {
          unityInstance.SendMessage(gameObjectName, callbackMethodName, "telegram_unavailable");
        }

        return;
      }

      window.Telegram.WebApp.openInvoice(url, function (status) {
        if (typeof unityInstance !== "undefined" && unityInstance != null) {
          unityInstance.SendMessage(gameObjectName, callbackMethodName, status || "");
        }
      });
    } catch (e) {
      console.error(e);
    }
  }
});
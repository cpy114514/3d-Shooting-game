mergeInto(LibraryManager.library, {
  BrowserSettingsSave: function (keyPtr, valuePtr) {
    try {
      var key = UTF8ToString(keyPtr);
      var value = UTF8ToString(valuePtr);
      window.localStorage.setItem(key, value);
    } catch (e) {
    }
  },

  BrowserSettingsLoad: function (keyPtr) {
    try {
      var key = UTF8ToString(keyPtr);
      var value = window.localStorage.getItem(key);
      if (value === null || value === undefined) {
        return 0;
      }

      var length = lengthBytesUTF8(value) + 1;
      var buffer = _malloc(length);
      stringToUTF8(value, buffer, length);
      return buffer;
    } catch (e) {
      return 0;
    }
  },

  BrowserSettingsFree: function (bufferPtr) {
    if (bufferPtr) {
      _free(bufferPtr);
    }
  }
});

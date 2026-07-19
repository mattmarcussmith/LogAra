(() => {
  const dbName = "logara-client";
  const storeName = "state";
  const stateKey = "snapshot";
  const cacheKey = "logara-client-state-cache";

  let cachedState = null;

  try {
    const cachedJson = window.localStorage.getItem(cacheKey);
    if (cachedJson) {
      cachedState = cachedJson;
    }
  } catch {
    cachedState = null;
  }

  function openDb() {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(dbName, 1);
      request.onupgradeneeded = () => {
        const db = request.result;
        if (!db.objectStoreNames.contains(storeName)) {
          db.createObjectStore(storeName);
        }
      };
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
  }

  async function readState() {
    const db = await openDb();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(storeName, "readonly");
      const store = tx.objectStore(storeName);
      const request = store.get(stateKey);
      request.onsuccess = () => resolve(request.result ?? null);
      request.onerror = () => reject(request.error);
    });
  }

  async function writeState(value) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(storeName, "readwrite");
      const store = tx.objectStore(storeName);
      const request = store.put(value, stateKey);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  function getThemeFromStateJson(json) {
    if (!json) {
      return "light";
    }

    try {
      const snapshot = JSON.parse(json);
      return snapshot?.Theme === "dark" ? "dark" : "light";
    } catch {
      return "light";
    }
  }

  function setTheme(theme) {
    const normalizedTheme = theme === "dark" ? "dark" : "light";
    document.documentElement.setAttribute("data-theme", normalizedTheme);
  }

  // Apply the cached theme immediately while Blazor is booting.
  setTheme(getThemeFromStateJson(cachedState));

  window.logaraStorage = {
    async loadState() {
      if (cachedState) {
        return cachedState;
      }

      const stateFromIndexedDb = await readState();
      if (stateFromIndexedDb) {
        cachedState = stateFromIndexedDb;
        try {
          window.localStorage.setItem(cacheKey, stateFromIndexedDb);
        } catch {
          // Ignore localStorage write failures.
        }
      }

      return stateFromIndexedDb;
    },
    async saveState(json) {
      cachedState = json;
      try {
        window.localStorage.setItem(cacheKey, json);
      } catch {
        // Ignore localStorage write failures.
      }

      await writeState(json);
    }
  };

  window.logaraTheme = {
    apply(theme) {
      setTheme(theme);
    }
  };
})();

const storageKey = "pdp-frontend";
function getStorage(): { [key: string]: any } {
    let storage = localStorage.getItem(storageKey);
    if (storage) {
        return JSON.parse(atob(storage));
    }
    return {};
}
function saveStorage(storage: { [key: string]: any }) {
    localStorage.setItem(storageKey, btoa(JSON.stringify(storage)));
}
/**
 * Gets a cookie by name
 * @param name The name of the cookie
 * @returns The value and ttl of the cookie
 */
export function getCookie(name: string): { value: string, ttl: Date } | undefined {
    let cookies: { [name: string]: { value: string, ttl: Date } } = getStorage()["cookies"] || {};
    return cookies[name];
}

/**
 * Sets a cookie
 * @param name The name of the cookie
 * @param value The value of the cookie
 * @param ttl The time to live of the cookie
 */
export function setCookie(name: string, value: string, ttl: number) {
    let date = new Date();
    date.setSeconds(date.getSeconds() + ttl);
    let storage = getStorage();
    storage["cookies"] = storage["cookies"] || {};
    storage["cookies"][name.trim()] = { value: value, ttl: date };
    saveStorage(storage);
}

/**
 * Deletes a cookie by name
 * @param name The name of the cookie
 */
export function deleteCookie(name: string) {
    let storage = getStorage();
    if (!storage["cookies"]) {
        return;
    }
    delete storage["cookies"][name];
    saveStorage(storage);
}

/**
 * Deletes all cookies
 */
export function deleteAll() {
    let storage = getStorage();
    delete storage["cookies"];
    saveStorage(storage);
}
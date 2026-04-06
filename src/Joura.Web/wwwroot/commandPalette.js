let registeredInstance;
let isOpen = false;

function isTypingTarget(target) {
    if (!(target instanceof HTMLElement)) {
        return false;
    }

    const tagName = target.tagName;
    return target.isContentEditable
        || tagName === "INPUT"
        || tagName === "TEXTAREA"
        || tagName === "SELECT";
}

function handleKeyDown(event) {
    if (!registeredInstance) {
        return;
    }

    const openShortcut = (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k";
    const slashShortcut = event.key === "/" && !event.metaKey && !event.ctrlKey && !event.altKey && !isTypingTarget(event.target);

    if (openShortcut || slashShortcut) {
        event.preventDefault();
        registeredInstance.invokeMethodAsync("ToggleAsync");
        return;
    }

    if (!isOpen) {
        return;
    }

    if (event.key === "Escape") {
        registeredInstance.invokeMethodAsync("CloseFromShortcutAsync");
        return;
    }

    if (event.key === "ArrowDown") {
        registeredInstance.invokeMethodAsync("MoveSelectionAsync", 1);
        event.preventDefault();
        return;
    }

    if (event.key === "ArrowUp") {
        registeredInstance.invokeMethodAsync("MoveSelectionAsync", -1);
        event.preventDefault();
        return;
    }

    if (event.key === "Enter") {
        registeredInstance.invokeMethodAsync("ExecuteSelectionAsync");
    }
}

export function initialize(dotNetReference) {
    registeredInstance = dotNetReference;
    window.addEventListener("keydown", handleKeyDown);
}

export function setOpenState(value) {
    isOpen = value === true;
}

export function setInputValue(element, value) {
    if (!element) {
        return;
    }

    element.focus();
    element.value = value ?? "";
    element.setSelectionRange(element.value.length, element.value.length);
}

export function dispose() {
    window.removeEventListener("keydown", handleKeyDown);
    registeredInstance = undefined;
    isOpen = false;
}

let accountMenuRoot;
let accountMenuDotNetRef;
let accountMenuPointerHandler;

function handleAccountMenuPointerDown(event) {
	if (!accountMenuRoot) {
		return;
	}

	const target = event.target;
	if (target instanceof Node && accountMenuRoot.contains(target)) {
		return;
	}

	accountMenuDotNetRef?.invokeMethodAsync("CloseAccountMenuFromJs");
}

export function registerAccountMenuOutsideClick(rootElement, dotNetRef) {
	unregisterAccountMenuOutsideClick();

	accountMenuRoot = rootElement;
	accountMenuDotNetRef = dotNetRef;
	accountMenuPointerHandler = handleAccountMenuPointerDown;
	document.addEventListener("pointerdown", accountMenuPointerHandler, true);
}

export function unregisterAccountMenuOutsideClick() {
	if (accountMenuPointerHandler) {
		document.removeEventListener("pointerdown", accountMenuPointerHandler, true);
	}

	accountMenuRoot = null;
	accountMenuDotNetRef = null;
	accountMenuPointerHandler = null;
}

export function disposeAccountMenuOutsideClick() {
	unregisterAccountMenuOutsideClick();
}

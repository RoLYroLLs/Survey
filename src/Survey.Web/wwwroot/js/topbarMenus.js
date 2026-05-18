function closeOpenTopbarMenus(exceptMenu) {
	document.querySelectorAll(".topbar__menu[open]").forEach(menu => {
		if (menu !== exceptMenu) {
			menu.removeAttribute("open");
		}
	});
}

document.addEventListener("toggle", event => {
	const menu = event.target;
	if (!(menu instanceof HTMLDetailsElement) || !menu.classList.contains("topbar__menu")) {
		return;
	}

	if (menu.open) {
		closeOpenTopbarMenus(menu);
	}
}, true);

document.addEventListener("pointerdown", event => {
	const target = event.target;
	if (!(target instanceof Node)) {
		return;
	}

	if (target.parentElement?.closest(".topbar__menu")) {
		return;
	}

	closeOpenTopbarMenus(null);
}, true);

document.addEventListener("click", event => {
	const target = event.target;
	if (!(target instanceof Element)) {
		return;
	}

	const actionable = target.closest(".topbar__tenant-option, .topbar__menu-item");
	if (!actionable || actionable.closest("summary")) {
		return;
	}

	const menu = actionable.closest(".topbar__menu");
	if (menu instanceof HTMLDetailsElement) {
		window.setTimeout(() => menu.removeAttribute("open"), 0);
	}
}, true);

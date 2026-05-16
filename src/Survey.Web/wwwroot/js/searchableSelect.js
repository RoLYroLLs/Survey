let activeControlElement = null;
let activeMenuElement = null;
let activeScrollHandler = null;
let activeResizeHandler = null;
let activeMenuWidth = null;

function positionActiveMenu() {
	if (!activeControlElement || !activeMenuElement) {
		return;
	}

	const rect = activeControlElement.getBoundingClientRect();
	const viewportPadding = 16;
	const menuGap = 4;
	const width = Math.min(Math.max(rect.width, activeMenuWidth ?? rect.width), window.innerWidth - (viewportPadding * 2));
	const left = Math.min(
		Math.max(viewportPadding, rect.left),
		Math.max(viewportPadding, window.innerWidth - width - viewportPadding)
	);
	const spaceBelow = window.innerHeight - rect.bottom - viewportPadding - menuGap;
	const spaceAbove = rect.top - viewportPadding - menuGap;
	const renderAbove = spaceBelow < 220 && spaceAbove > spaceBelow;
	const maxHeight = Math.max(120, renderAbove ? spaceAbove : spaceBelow);

	activeMenuElement.style.position = "fixed";
	activeMenuElement.style.left = `${left}px`;
	activeMenuElement.style.width = `${width}px`;
	activeMenuElement.style.minWidth = `${rect.width}px`;
	activeMenuElement.style.maxHeight = `${maxHeight}px`;

	if (renderAbove) {
		activeMenuElement.style.top = "auto";
		activeMenuElement.style.bottom = `${window.innerHeight - rect.top + menuGap}px`;
	}
	else {
		activeMenuElement.style.top = `${rect.bottom + menuGap}px`;
		activeMenuElement.style.bottom = "auto";
	}

	activeMenuElement.style.visibility = "visible";
	activeMenuElement.style.opacity = "1";
}

function measureMenuWidth(controlElement, menuElement) {
	const optionElements = Array.from(menuElement.querySelectorAll(".searchable-select__option, .searchable-select__empty"));
	const previousWidth = menuElement.style.width;
	const previousMinWidth = menuElement.style.minWidth;
	const previousVisibility = menuElement.style.visibility;
	const previousOpacity = menuElement.style.opacity;

	menuElement.style.width = "auto";
	menuElement.style.minWidth = "0";
	menuElement.style.visibility = "hidden";
	menuElement.style.opacity = "0";

	const widestOption = optionElements.reduce((maxWidth, element) => {
		return Math.max(maxWidth, element.scrollWidth);
	}, 0);

	menuElement.style.width = previousWidth;
	menuElement.style.minWidth = previousMinWidth;
	menuElement.style.visibility = previousVisibility;
	menuElement.style.opacity = previousOpacity;

	return Math.max(controlElement.getBoundingClientRect().width, widestOption + 24);
}

function clearActiveMenuListeners() {
	if (activeScrollHandler) {
		window.removeEventListener("scroll", activeScrollHandler, true);
		activeScrollHandler = null;
	}

	if (activeResizeHandler) {
		window.removeEventListener("resize", activeResizeHandler, true);
		activeResizeHandler = null;
	}
}

function getTabbableElements() {
	const selector = [
		"a[href]",
		"button:not([disabled])",
		"input:not([disabled])",
		"select:not([disabled])",
		"textarea:not([disabled])",
		"[tabindex]:not([tabindex='-1'])"
	].join(", ");

	return Array.from(document.querySelectorAll(selector))
		.filter((element) => {
			if (!(element instanceof HTMLElement)) {
				return false;
			}

			if (element.hidden || element.getAttribute("aria-hidden") === "true") {
				return false;
			}

			const style = window.getComputedStyle(element);
			if (style.display === "none" || style.visibility === "hidden") {
				return false;
			}

			return element.offsetParent !== null || style.position === "fixed";
		});
}

function focusRelativeTabStop(currentElement, backwards) {
	if (!(currentElement instanceof HTMLElement)) {
		return;
	}

	const tabbableElements = getTabbableElements();
	const currentIndex = tabbableElements.indexOf(currentElement);
	if (currentIndex < 0) {
		return;
	}

	const nextIndex = backwards ? currentIndex - 1 : currentIndex + 1;
	const nextElement = tabbableElements[nextIndex];
	if (nextElement instanceof HTMLElement) {
		nextElement.focus();
	}
}

window.surveySearchableSelect = window.surveySearchableSelect || {
	positionMenu(controlElement, menuElement) {
		activeControlElement = controlElement;
		activeMenuElement = menuElement;

		clearActiveMenuListeners();

		activeScrollHandler = () => positionActiveMenu();
		activeResizeHandler = () => positionActiveMenu();
		window.addEventListener("scroll", activeScrollHandler, true);
		window.addEventListener("resize", activeResizeHandler, true);

		activeMenuWidth = measureMenuWidth(controlElement, menuElement);
		positionActiveMenu();
	},

	registerInputKeyHandler(inputElement, dotNetRef) {
		if (!inputElement || inputElement.__searchableSelectKeyHandler) {
			return;
		}

		const handler = (event) => {
			const isOpen = !!inputElement.closest(".searchable-select--open");
			if (event.key === "Tab" && isOpen) {
				event.preventDefault();
				dotNetRef.invokeMethodAsync("HandleSpecialKeyFromJs", "Tab")
					.finally(() => focusRelativeTabStop(inputElement, event.shiftKey));
				return;
			}

			if (event.key === "ArrowDown" || event.key === "ArrowUp" || event.key === "Enter" || event.key === "Escape") {
				event.preventDefault();
				dotNetRef.invokeMethodAsync("HandleSpecialKeyFromJs", event.key);
			}
		};

		inputElement.__searchableSelectKeyHandler = handler;
		inputElement.addEventListener("keydown", handler);
	},

	unregisterInputKeyHandler(inputElement) {
		if (!inputElement || !inputElement.__searchableSelectKeyHandler) {
			return;
		}

		inputElement.removeEventListener("keydown", inputElement.__searchableSelectKeyHandler);
		delete inputElement.__searchableSelectKeyHandler;
	},

	selectInputText(inputElement) {
		if (!inputElement) {
			return;
		}

		inputElement.select();
	},

	blurInput(inputElement) {
		if (!inputElement) {
			return;
		}

		inputElement.blur();
	},

	updateHighlightedOption(menuElement, highlightedIndex) {
		if (!menuElement || highlightedIndex < 0) {
			if (menuElement) {
				menuElement.querySelectorAll(".searchable-select__option--highlighted").forEach((element) => {
					element.classList.remove("searchable-select__option--highlighted");
				});
			}
			return;
		}

		const optionElements = Array.from(menuElement.querySelectorAll(".searchable-select__option"));
		const targetOption = optionElements[highlightedIndex];
		if (!targetOption) {
			return;
		}

		optionElements.forEach((element, index) => {
			if (index === highlightedIndex) {
				element.classList.add("searchable-select__option--highlighted");
			}
			else {
				element.classList.remove("searchable-select__option--highlighted");
			}
		});

		targetOption.scrollIntoView({
			block: "nearest"
		});
	},

	clearMenu() {
		clearActiveMenuListeners();
		activeControlElement = null;
		activeMenuElement = null;
		activeMenuWidth = null;
	}
};

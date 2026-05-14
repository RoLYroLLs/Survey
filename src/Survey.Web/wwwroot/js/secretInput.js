window.SurveySecretInput = window.SurveySecretInput || {
	toggle(button) {
		const container = button.closest(".secret-input");
		if (!container) {
			return;
		}

		const input = container.querySelector("[data-secret-input-field='true']");
		if (!(input instanceof HTMLInputElement)) {
			return;
		}

		const isVisible = input.type === "text";
		input.type = isVisible ? "password" : "text";
		button.setAttribute("aria-pressed", isVisible ? "false" : "true");
		button.setAttribute("aria-label", isVisible
			? (button.dataset.showLabel || "Show value")
			: (button.dataset.hideLabel || "Hide value"));
		container.classList.toggle("secret-input--visible", !isVisible);
	}
};

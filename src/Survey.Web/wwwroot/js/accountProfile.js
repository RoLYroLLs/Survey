window.surveyToggleOrganizationField = function (selectElement) {
  if (!selectElement) {
    return;
  }

  const form = selectElement.closest("form");
  const organizationField = form?.querySelector("[data-organization-field]");
  const organizationInput = organizationField?.querySelector("input");
  const isOrganization = selectElement.value === "Organization";

  if (organizationField) {
    organizationField.classList.toggle("field-stack--ghost", !isOrganization);
  }

  if (!isOrganization && organizationInput) {
    organizationInput.value = "";
  }
};

window.addEventListener("DOMContentLoaded", function () {
  document
    .querySelectorAll("[data-account-type-select]")
    .forEach(function (selectElement) {
      window.surveyToggleOrganizationField(selectElement);
    });
});

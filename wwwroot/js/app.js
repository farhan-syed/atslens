const form = document.getElementById("analyzer-form");
const analyzeBtn = document.getElementById("analyzeBtn");
const formError = document.getElementById("formError");
const results = document.getElementById("results");

const visibilityStatus = document.getElementById("visibilityStatus");
const quickChecks = document.getElementById("quickChecks");
const priorityFixes = document.getElementById("priorityFixes");
const matchedKeywords = document.getElementById("matchedKeywords");
const missingKeywords = document.getElementById("missingKeywords");
const skillsOutput = document.getElementById("skillsOutput");

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  formError.hidden = true;
  formError.textContent = "";
  analyzeBtn.disabled = true;

  const payload = {
    targetTitle: document.getElementById("targetTitle").value,
    jobDescription: document.getElementById("jobDescription").value,
    resumeText: document.getElementById("resumeText").value,
    isPdfSelectableText: document.getElementById("pdfReadable").checked,
  };

  if (!payload.jobDescription.trim() || !payload.resumeText.trim()) {
    analyzeBtn.disabled = false;
    formError.hidden = false;
    formError.textContent = "Please provide both job description and resume text.";
    return;
  }

  try {
    const response = await fetch("/api/analyze", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const body = await response.json();
      throw new Error(body.error || "Analysis failed.");
    }

    const data = await response.json();
    render(data);
  } catch (error) {
    formError.hidden = false;
    formError.textContent = error.message;
  } finally {
    analyzeBtn.disabled = false;
  }
});

function render(data) {
  if (results.hidden) {
    results.hidden = false;
    requestAnimationFrame(() => {
      results.classList.add("is-visible");
    });
  }

  visibilityStatus.textContent = data.visibilityStatus;
  visibilityStatus.classList.toggle("at-risk", data.visibilityStatus === "At Risk");

  quickChecks.replaceChildren();
  appendListItem(quickChecks, `Title exact match: ${data.titleExactMatch ? "Yes" : "No"}`);
  appendListItem(quickChecks, `PDF text selectable: ${data.resumeLikelyReadable ? "Yes" : "No"}`);
  appendListItem(quickChecks, `Hard skills detected: ${data.hardSkillCountInResume}`);

  fillSimpleList(priorityFixes, data.priorityFixes);
  fillPillList(matchedKeywords, data.matchedKeywords);
  fillPillList(missingKeywords, data.missingKeywords);

  skillsOutput.value = data.suggestedSkillsSection.join(" | ");
}

function fillSimpleList(listNode, items) {
  listNode.replaceChildren();
  if (!items || items.length === 0) {
    appendListItem(listNode, "None");
    return;
  }

  items.forEach((item) => appendListItem(listNode, item));
}

function fillPillList(listNode, items) {
  listNode.replaceChildren();
  if (!items || items.length === 0) {
    appendListItem(listNode, "None found");
    return;
  }

  items.forEach((item) => {
    const li = document.createElement("li");
    li.textContent = item;
    listNode.appendChild(li);
  });
}

function appendListItem(listNode, value) {
  const li = document.createElement("li");
  li.textContent = value;
  listNode.appendChild(li);
}

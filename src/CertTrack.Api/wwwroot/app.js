// CertTrack front end. It talks to the API with fetch and builds the page with DOM methods.
// Text from the user is always set with textContent, never innerHTML, so it cannot run as HTML.

const TOKEN_KEY = "certtrack.token";
const EMAIL_KEY = "certtrack.email";
const STATUS_LABELS = { NotStarted: "Not started", Studying: "Studying", Done: "Done" };

const $ = (id) => document.getElementById(id);

let token = localStorage.getItem(TOKEN_KEY);
let email = localStorage.getItem(EMAIL_KEY);
let registering = false;
let editingId = null;

// ---------- Talking to the API ----------

async function api(method, path, body) {
  const headers = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (token) headers["Authorization"] = `Bearer ${token}`;

  let response;
  try {
    response = await fetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  } catch {
    throw new Error("Could not reach the server.");
  }

  // A 401 while logged in means the token expired or is no longer valid.
  if (response.status === 401 && token) {
    logOut();
    throw new Error("Your session has ended. Log in again.");
  }

  if (response.status === 204) return null;

  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(problemMessage(data));
  return data;
}

// Turns the API's error body into one line of text.
function problemMessage(problem) {
  if (problem && problem.errors) return Object.values(problem.errors).flat().join(" ");
  if (problem && problem.title) return problem.title;
  return "Something went wrong.";
}

// Runs an action and shows its error, if any. Returns true when it worked.
async function run(action) {
  showMessage("");
  try {
    await action();
    return true;
  } catch (error) {
    showMessage(error.message);
    return false;
  }
}

function showMessage(text) {
  $("message").textContent = text;
  $("message").hidden = text === "";
}

// ---------- Logging in and out ----------

function logIn(auth) {
  token = auth.token;
  email = auth.email;
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(EMAIL_KEY, email);
}

function logOut() {
  token = null;
  email = null;
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EMAIL_KEY);
  showView();
}

function showView() {
  const loggedIn = token !== null;
  $("auth-view").hidden = loggedIn;
  $("app-view").hidden = !loggedIn;
  $("session").hidden = !loggedIn;
  $("user-email").textContent = email ?? "";

  $("auth-title").textContent = registering ? "Create an account" : "Log in";
  $("auth-submit").textContent = registering ? "Create account" : "Log in";
  $("auth-switch").textContent = registering ? "Log in instead" : "Create an account instead";
  $("auth-password").autocomplete = registering ? "new-password" : "current-password";
  $("auth-password").minLength = registering ? 8 : 0;
}

$("auth-switch").addEventListener("click", () => {
  registering = !registering;
  showMessage("");
  showView();
});

$("auth-form").addEventListener("submit", (event) => {
  event.preventDefault();
  run(async () => {
    const path = registering ? "/api/auth/register" : "/api/auth/login";
    logIn(await api("POST", path, { email: $("auth-email").value, password: $("auth-password").value }));
    $("auth-form").reset();
    resetForm();
    showView();
    await refresh();
  });
});

$("logout").addEventListener("click", () => {
  showMessage("");
  logOut();
});

// ---------- Loading and showing the data ----------

async function refresh() {
  const filters = new URLSearchParams();
  if ($("filter-cert").value) filters.set("cert", $("filter-cert").value);
  if ($("filter-status").value) filters.set("status", $("filter-status").value);

  const [stats, objectives] = await Promise.all([
    api("GET", "/api/stats"),
    api("GET", `/api/objectives?${filters}`)
  ]);

  renderStats(stats);
  renderCertFilter(stats);
  renderObjectives(objectives);
}

function element(tag, text, className) {
  const node = document.createElement(tag);
  if (text !== undefined) node.textContent = text;
  if (className) node.className = className;
  return node;
}

function renderStats(stats) {
  const container = $("stats");
  container.replaceChildren();

  if (stats.length === 0) {
    container.append(element("p", "Add your first objective to see your progress.", "muted"));
    return;
  }

  for (const cert of new Set(stats.map((s) => s.cert))) {
    const domains = stats.filter((s) => s.cert === cert);
    const done = domains.reduce((sum, s) => sum + s.done, 0);
    const total = domains.reduce((sum, s) => sum + s.total, 0);

    container.append(element("h3", `${cert}: ${done} of ${total} done`));
    for (const domain of domains) {
      const row = element("div", undefined, "stat");
      const bar = element("progress");
      bar.max = domain.total;
      bar.value = domain.done;

      row.append(
        element("span", `Domain ${domain.domain}`),
        bar,
        element("span", `${domain.done}/${domain.total} (${Math.round(domain.percent)}%)`, "muted"));
      container.append(row);
    }
  }
}

// Fills the cert filter with the certs the user has, keeping the current choice.
function renderCertFilter(stats) {
  const select = $("filter-cert");
  const chosen = select.value;
  const certs = [...new Set(stats.map((s) => s.cert))];

  select.replaceChildren(new Option("All", ""));
  for (const cert of certs) select.append(new Option(cert, cert));
  select.value = certs.includes(chosen) ? chosen : "";
}

function renderObjectives(objectives) {
  const body = $("objectives");
  body.replaceChildren();
  $("objectives-table").hidden = objectives.length === 0;
  $("empty").hidden = objectives.length !== 0;

  for (const objective of objectives) {
    const row = element("tr");
    row.append(
      element("td", objective.cert),
      element("td", String(objective.domain)),
      element("td", objective.title, "title"),
      cell(statusSelect(objective)),
      element("td", objective.lastReviewed ? new Date(objective.lastReviewed).toLocaleDateString() : "Never", "muted"),
      cell(editButton(objective), deleteButton(objective)));
    body.append(row);
  }
}

function cell(...children) {
  const td = element("td", undefined, "actions");
  td.append(...children);
  return td;
}

function statusSelect(objective) {
  const select = element("select", undefined, `status ${objective.status}`);
  select.setAttribute("aria-label", `Status of ${objective.title}`);
  for (const [value, label] of Object.entries(STATUS_LABELS)) select.append(new Option(label, value));
  select.value = objective.status;

  select.addEventListener("change", async () => {
    const saved = await run(() => api("PUT", `/api/objectives/${objective.id}`, { ...objective, status: select.value }));
    if (!saved) select.value = objective.status;
    else await run(refresh);
  });
  return select;
}

function editButton(objective) {
  const button = element("button", "Edit", "quiet");
  button.type = "button";
  button.addEventListener("click", () => {
    editingId = objective.id;
    $("objective-title").value = objective.title;
    $("objective-cert").value = objective.cert;
    $("objective-domain").value = objective.domain;
    $("objective-status").value = objective.status;
    showForm();
    $("objective-title").focus();
  });
  return button;
}

function deleteButton(objective) {
  const button = element("button", "Delete", "quiet danger");
  button.type = "button";
  button.addEventListener("click", () => {
    if (!confirm(`Delete "${objective.title}"?`)) return;
    run(async () => {
      await api("DELETE", `/api/objectives/${objective.id}`);
      if (editingId === objective.id) resetForm();
      await refresh();
    });
  });
  return button;
}

// ---------- The add and edit form ----------

function showForm() {
  const editing = editingId !== null;
  $("form-title").textContent = editing ? "Edit objective" : "Add an objective";
  $("objective-submit").textContent = editing ? "Save" : "Add";
  $("objective-cancel").hidden = !editing;
}

function resetForm() {
  editingId = null;
  $("objective-form").reset();
  showForm();
}

$("objective-cancel").addEventListener("click", resetForm);

$("objective-form").addEventListener("submit", (event) => {
  event.preventDefault();
  run(async () => {
    const objective = {
      title: $("objective-title").value,
      cert: $("objective-cert").value,
      domain: Number($("objective-domain").value),
      status: $("objective-status").value
    };

    if (editingId === null) {
      await api("POST", "/api/objectives", objective);
      // Keep the cert and domain, so several objectives of one domain are quick to add.
      $("objective-title").value = "";
      $("objective-status").value = "NotStarted";
      $("objective-title").focus();
    } else {
      await api("PUT", `/api/objectives/${editingId}`, objective);
      resetForm();
    }

    await refresh();
  });
});

$("filter-cert").addEventListener("change", () => run(refresh));
$("filter-status").addEventListener("change", () => run(refresh));

// ---------- Start ----------

showView();
if (token) run(refresh);

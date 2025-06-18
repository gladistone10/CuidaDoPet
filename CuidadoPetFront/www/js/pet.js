function showToast(message, type = "primary") {
  const toastContainer = document.getElementById("toastContainer");

  const toastEl = document.createElement("div");
  toastEl.className = `toast align-items-center text-bg-${type} border-0`;
  toastEl.setAttribute("role", "alert");
  toastEl.setAttribute("aria-live", "assertive");
  toastEl.setAttribute("aria-atomic", "true");

  toastEl.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">${message}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Fechar"></button>
    </div>
  `;

  toastContainer.appendChild(toastEl);

  const toast = new bootstrap.Toast(toastEl, { delay: 4000 });
  toast.show();

  toastEl.addEventListener("hidden.bs.toast", () => {
    toastEl.remove();
  });
}

async function getPet() {
  const id = document.getElementById("petId").value.trim();

  if (!id) {
    showToast("Por favor, informe o ID do pet.", "warning");
    return;
  }

  const fakeDB = {
    1: { name: "Rex", age: "5 anos", type: "Cachorro" },
    2: { name: "Mimi", age: "3 anos", type: "Gato" },
    3: { name: "Toby", age: "2 anos", type: "Coelho" },
  };

  const pet = fakeDB[id];

  if (!pet) {
    document.getElementById("petInfo").innerHTML = "";
    showToast("Pet não encontrado.", "danger");
    return;
  }

  document.getElementById("petInfo").innerHTML = `
    <div class="card-pet-info">
      <p><strong>Nome:</strong> ${pet.name}</p>
      <p><strong>Idade:</strong> ${pet.age}</p>
      <p><strong>Tipo:</strong> ${pet.type}</p>
    </div>
  `;
  showToast("Informações do pet carregadas com sucesso!", "success");
}

async function updatePet() {
  const id = document.getElementById("petId").value.trim();
  if (!id) {
    showToast("Informe o ID do pet para atualizar.", "warning");
    return;
  }

  const name = document.getElementById("petName").value.trim();
  const age = document.getElementById("petAge").value.trim();
  const type = document.getElementById("petType").value.trim();

  if (!name || !age || !type) {
    showToast("Preencha todos os campos para atualizar.", "warning");
    return;
  }

  showToast(`Pet ${id} atualizado com sucesso!`, "success");
}

async function trustPet() {
  const id = document.getElementById("petId").value.trim();
  const userId = document.getElementById("userId").value.trim();

  if (!id || !userId) {
    showToast("Informe o ID do pet e o ID do usuário para liberar.", "warning");
    return;
  }

  showToast(
    `Informações do pet ${id} liberadas para o usuário ${userId}.`,
    "info"
  );
}

async function deletePet() {
  const id = document.getElementById("petId").value.trim();

  if (!id) {
    showToast("Informe o ID do pet para remover.", "warning");
    return;
  }

  document.getElementById("petInfo").innerHTML = "";
  showToast(`Pet com ID ${id} removido com sucesso!`, "danger");
}

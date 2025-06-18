let pets = {};

fetch('http://localhost:5053/Pets', {
  headers: {
    Authorization: `Bearer ${getCookie("Token")}`
  }
})
  .then(response => {
    if (!response.ok) {
      throw new Error(`Erro na requisição: ${response.status}`);
    }
    return response.json();
  })
  .then(data => {
    pets = data;
    renderizarPets(pets);
  })
  .catch(error => {
    console.error('Erro ao buscar os dados:', error);
  });


function renderizarPets(lista) {
  const grid = document.getElementById("petGrid");
  grid.innerHTML = "";
  lista.forEach(pet => {
    const card = document.createElement("div");
    card.className = "col-6 col-md-4 col-lg-3";
    card.innerHTML = `
          <a href="Pet.html?nome=${encodeURIComponent(pet.name)}&id=${pet.id}" class="pet-card ${pet.gender === 'M' ? 'pet-male' : 'pet-female'}">
            <div class="pet-level">Pts. ${pet.points}</div>
            <img src="https://placedog.net/100/100?id=${pet.id}" alt="Imagem de ${pet.name}" class="pet-image" />
            <div class="pet-name">${pet.name}</div>
          </a>
        `;
    grid.appendChild(card);
  });
}

function ordenarPets() {
  pets.sort((a, b) => a.nome.localeCompare(b.nome));
  renderizarPets(pets);
}

function pesquisarPets() {
  const termo = prompt("Digite o nome do pet:");
  const resultado = pets.filter(p => p.nome.toLowerCase().includes(termo.toLowerCase()));
  renderizarPets(resultado);
}

const cuidados = {
      cachorro: [
        "Verifique sinais vitais e mantenha o cão calmo.",
        "Imobilize fraturas com cuidado.",
        "Evite que o cão lamba feridas abertas."
      ],
      gato: [
        "Mantenha o gato aquecido em caso de choque.",
        "Nunca dê medicamentos humanos sem orientação.",
        "Limpe feridas com soro fisiológico."
      ]
    };

    const detalhes = {
      cachorro: `
        <h2 class="mb-4">🐶 Cachorro — Situações e Primeiros Cuidados</h2>
        <div class="accordion" id="accordionDog">
          <div class="accordion-item">
            <h2 class="accordion-header" id="dog1Heading">
              <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#dog1" aria-expanded="false" aria-controls="dog1">
                1. Chegada no novo lar
              </button>
            </h2>
            <div id="dog1" class="accordion-collapse collapse" aria-labelledby="dog1Heading" data-bs-parent="#accordionDog">
              <div class="accordion-body">
                <ul>
                  <li>Prepare um espaço calmo com cama, potes de água e comida.</li>
                  <li>Evite barulhos altos e contato excessivo nos primeiros dias.</li>
                  <li>Leve ao veterinário para check-up, vacinas e vermífugo.</li>
                </ul>
              </div>
            </div>
          </div>
          <!-- Adicione mais situações aqui -->
        </div>
      `,
      gato: `
        <h2 class="mb-4">🐱 Gato — Situações e Primeiros Cuidados</h2>
        <div class="accordion" id="accordionCat">
          <div class="accordion-item">
            <h2 class="accordion-header" id="cat1Heading">
              <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#cat1" aria-expanded="false" aria-controls="cat1">
                1. Chegada no novo lar
              </button>
            </h2>
            <div id="cat1" class="accordion-collapse collapse" aria-labelledby="cat1Heading" data-bs-parent="#accordionCat">
              <div class="accordion-body">
                <ul>
                  <li>Separe um cômodo calmo com caixa de areia, água e comida.</li>
                  <li>Evite forçar contato físico nos primeiros dias.</li>
                  <li>Leve ao veterinário para check-up, vacinação e vermífugo.</li>
                </ul>
              </div>
            </div>
          </div>
          <!-- Adicione mais situações aqui -->
        </div>
      `
    };

    let animalAtual = '';
    let todosCuidados = [];

    function mostrarCuidados(animal) {
      animalAtual = animal;
      todosCuidados = cuidados[animal];
      document.getElementById('cardsAnimais').classList.add('d-none');
      document.getElementById('secaoCuidados').classList.remove('d-none');
      document.getElementById('tituloAnimal').textContent = `Cuidados com ${animal}`;
      renderizarLista(todosCuidados);
      document.getElementById('cuidadosDetalhados').innerHTML = detalhes[animal];
    }

    function renderizarLista(lista) {
      const ul = document.getElementById('listaCuidados');
      ul.innerHTML = '';
      if (lista.length === 0) {
        ul.innerHTML = '<li class="list-group-item">Nenhum cuidado encontrado.</li>';
        return;
      }
      lista.forEach(item => {
        const li = document.createElement('li');
        li.className = 'list-group-item';
        li.textContent = item;
        ul.appendChild(li);
      });
    }

    function filtrarCuidados() {
      const termo = document.getElementById('campoBusca').value.toLowerCase();
      if (!animalAtual) return;
      const filtrados = todosCuidados.filter(cuidado =>
        cuidado.toLowerCase().includes(termo)
      );
      renderizarLista(filtrados);
    }

    function voltar() {
      document.getElementById('secaoCuidados').classList.add('d-none');
      document.getElementById('cardsAnimais').classList.remove('d-none');
      document.getElementById('campoBusca').value = '';
      document.getElementById('cuidadosDetalhados').innerHTML = '';
    }
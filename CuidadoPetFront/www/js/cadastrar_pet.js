// Função para mostrar o toast com mensagem dinâmica
function mostrarToast(mensagem) {
  const toastEl = document.getElementById("liveToast");
  const toastBody = document.getElementById("toastBody");
  toastBody.textContent = mensagem;

  const toast = new bootstrap.Toast(toastEl);
  toast.show();
}

function getCookie(name) {
  var nameEQ = name + "=";
  var ca = document.cookie.split(';');
  for (var i = 0; i < ca.length; i++) {
    var c = ca[i];
    while (c.charAt(0) == ' ') c = c.substring(1, c.length);
    if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
  }
  return null;
}

// Validar e processar o envio do formulário
const Cadastrar = (btn) => {
  btn.disabled = true;
  const nome = document.getElementById("nomePet").value.trim();
  const raca = document.getElementById("racaPet").value.trim();
  const rawData = document.getElementById("idadePet").value;
  const dataObj = new Date(rawData);
  const yyyy = dataObj.getFullYear();
  const mm = String(dataObj.getMonth() + 1).padStart(2, '0');
  const dd = String(dataObj.getDate()).padStart(2, '0');
  const dataFormatada = `${yyyy}-${mm}-${dd}`;

  const sexoRadio = document.querySelector('input[name="sexoPet"]:checked');

  if (!nome || !raca || !dataFormatada || !sexoRadio) {
    mostrarToast(
      "Por favor, preencha todos os campos obrigatórios e selecione o sexo."
    );
    btn.disabled = false;
    return;
  }


  fetch('http://localhost:5053/Pets', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
      Authorization: `Bearer ${getCookie("Token")}`
      
    },
    body: new URLSearchParams({
      name: nome,
      breed: raca,
      birthdate: dataFormatada,
      gender: sexoRadio.value
    })
  })

  mostrarToast("Cadastro realizado com sucesso!");
  window.location = "/ListaPet.html";
  btn.disabled = false;
};

// Botão Voltar
document.getElementById("btnVoltar").addEventListener("click", function () {
  window.location = "ListaPet.html";
});

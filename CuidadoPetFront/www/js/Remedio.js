document.getElementById("cadastroForm").addEventListener("submit", function (event) {
  event.preventDefault();

  const tipo = document.getElementById("tipoServico").value;
  const descricao = document.getElementById("descricao").value;
  const rawData = document.getElementById("data").value;
  const dataObj = new Date(rawData);
  const yyyy = dataObj.getFullYear();
  const mm = String(dataObj.getMonth() + 1).padStart(2, '0');
  const dd = String(dataObj.getDate()).padStart(2, '0');
  const dataFormatada = `${yyyy}-${mm}-${dd}`;


  if (!tipo || !data) {
    alert("Por favor, preencha todos os campos obrigatórios.");
    return;
  }

  if (tipo == "vacina") {
    fetch(`http://localhost:5053/care/${getCookie("petId")}/vaccination`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        Authorization: `Bearer ${getCookie("Token")}`
      },
      body: new URLSearchParams({
        VaccineName: descricao,
        VaccineDate: dataFormatada,
        ExpirationDate: dataFormatada
      })
    })

    document.getElementById("mensagemSucesso").style.display = "block";
  }
  this.reset();
});



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

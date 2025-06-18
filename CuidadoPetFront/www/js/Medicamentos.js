 document.getElementById("formCadastro").addEventListener("submit", function(event) {
    event.preventDefault();

    const petName = document.getElementById("petName").value.trim();
    const medName = document.getElementById("medName").value.trim();
    const dosagem = document.getElementById("dosagem").value.trim();
    const inicio = document.getElementById("inicioTratamento").value;
    const fim = document.getElementById("fimTratamento").value;

    if (!petName || !medName || !dosagem || !inicio || !fim) {
      alert("Por favor, preencha todos os campos obrigatórios.");
      return;
    }

    if (inicio > fim) {
      alert("A data de término deve ser posterior à data de início.");
      return;
    }

    alert("Tratamento salvo com sucesso! ✅");

    // Aqui você pode adicionar lógica para salvar em banco de dados, localStorage, etc.
    document.getElementById("formCadastro").reset();
  });
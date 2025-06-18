

# Cuidado Pet
**Uma Plataforma Web/Mobile Profissional para Gestão de Pets — Front-end + Cordova**

---

## Visão Geral

O **Cuidado Pet** é uma solução web e mobile completa para tutores e profissionais veterinários registrarem, acompanharem e gerenciarem informações clínicas, de rotina e emergência de animais de estimação. Desenvolvido com **HTML5, CSS3, JavaScript ES6**, integra **Bootstrap 5** para UI responsiva moderna e utiliza **Apache Cordova** para distribuição multiplataforma (Android/iOS), mantendo alto padrão visual, segurança, acessibilidade (WCAG AA) e experiência nativa no dispositivo.

Ideal para demonstração acadêmica, portfólio profissional e aplicações reais, o projeto foca em arquitetura de código modular, práticas modernas de usabilidade e integração transparente com serviços backend via API RESTful.

---

## Sumário

- [Motivação & Objetivo](#motivação--objetivo)
- [Funcionalidades Principais](#funcionalidades-principais)
- [Arquitetura & Cordova](#arquitetura--cordova)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Como Executar (Web e Mobile)](#como-executar-web-e-mobile)
- [Experiência Visual (UI/UX)](#experiência-visual-uiux)
- [Acessibilidade e Responsividade](#acessibilidade-e-responsividade)
- [Demonstração Visual](#demonstração-visual)
- [Contribuição & Contato](#contribuição--contato)
- [Licença](#licença)

---

## Motivação & Objetivo

- Centralizar dados clínicos de múltiplos pets em um app intuitivo;
- Reduzir riscos de esquecimentos em vacinas, e primeiros socorros;
- Proporcionar interface que agrega valor real, moderna, profissional, tanto para web quanto mobile (App Store/Google Play-ready);
- Exemplificar integração de stack web + Cordova para aplicações híbridas, destacando práticas avançadas de engenharia de software, mobile UX e integração REST.

---

## Funcionalidades Principais

- **Login e Cadastro Seguros:**
  Autenticação pelo backend REST, persistência por token;
- **Multi-Pet, Multi-Funcionalidade:**
  Cadastro e consulta de pets, histórico de saúde, informações detalhadas;
- **Histórico Clínico & Vacinação:**
  Linha do tempo e detalhamento, cadastro, busca de vacinas;
- **Primeiros Socorros Dinâmicos:**
  Conteúdo interativo, busca para emergências, adaptativo por espécie/tipo pet;
- **Experiência Nativa Mobile (via Cordova):**
  Acesso a recursos do aparelho (camera, contatos, banco local, notificações em futuras releases);
- **UI/UX Modernos e Profissionais:**
  Glassmorphism, ícones modernos, navegação responsiva por cards, off-canvas menus;
- **Acessibilidade Avançada:**
  Estado visual de foco, alt em todas as imagens, teste em leitores de tela e teclado.

---

## Arquitetura & Cordova

- **App Cordova:**
  - Todo o código fonte fica na pasta `/www/` (estrutura compatível com Cordova);
  - Uso de comandos padrão Cordova para build, emulate e deploy mobile;
- **Código Modular e Manutenível:**
  - Arquitetura desacoplada: HTML para cada funcionalidade, imports CSS/JS específicos;
- **Backend-Ready:**
  - Requisições via Fetch API para endpoints RESTful (autenticação, pets, histórico);
- **Empacotamento Mobile:**
  - Cordova Plugins (nativos/opcionais): Camera, FileSystem, Notification etc.

---

## Tecnologias Utilizadas

- **HTML5, CSS3 (Bootstrap 5, custom utilities)**
- **JavaScript (ES6) — Vanilla, sem frameworks pesados**
- **Bootstrap Icons, Icons8**
- **Imagens com [placehold.co](https://placehold.co/) + alt text detalhado**
- **Apache Cordova**
    - Plugins oficiais e suporte a extensões nativas (em futuras versões)
    - Compatível com Android e iOS (via build Cordova)

---

## Como Executar (Web e Mobile)

### Como executar via navegador (Uso Web)
```bash
# Dê duplo clique em Login.html ou ListaPet.html
```

### Como empacotar via Cordova (Mobile App)
1. Instale Cordova globalmente:
   ```bash
   npm install -g cordova
   ```
2. Crie o projeto Cordova:
    ```bash
    cordova create cuidado-pet
    cd cuidado-pet
    ```
3. Copie o conteúdo da pasta `/www` deste repositório para a pasta `/www` do Cordova.
4. Adicione a plataforma desejada:
    ```bash
    cordova platform add android
    cordova platform add ios   # (no Mac)
    ```
5. Construa e rode no emulador/aparelho:
    ```bash
    cordova build android
    cordova emulate android
    ```
*Obs: backend precisa rodar em camada localização acessível pelo dispositivo/emulador.*

---

## Experiência Visual (UI/UX)

- **Design Avançado:**
    Glassmorphism, gradientes suaves, contrastes fortes, adaptação mobile-first.
- **Navegação:**
    Barra inferior fixada (mobile), navegação por cards/atalhos, menus contextuais.
- **Inputs e feedbacks modernos:**
    Validação em tempo real, tooltips, toasts Bootstrap, animações suaves.
- **Imagens (sempre com alt text):**
    ```html
    <img src="https://placehold.co/120x120?text=Avatar+do+Pet"
         alt="Avatar ilustrativo de pet realista, fundo azul, estilo cartoon moderno" />
    ```

---

## Acessibilidade e Responsividade

| Dispositivo | Destaques                                                         |
|-------------|-------------------------------------------------------------------|
| Mobile      | Layout coluna única, touch-friendly, font grande                  |
| Tablet      | Grid adaptativo, navegação fluida, cards expansivos               |

- 100% imagens com alt
- Caminho de foco visível para teclado
- Labels detalhadas, compatibilidade com leitores de tela
- Contraste mínimo atendendo ao WCAG AA em todas as telas

---

## Demonstração Visual

```html
<!-- Avatar Pet -->
<img src="https://placehold.co/120x120?text=Avatar+do+Pet"
     alt="Avatar modernista de cachorro feliz, cartoon realista, fundo azul" />
<!-- Cartão de Vacinação -->
<img src="https://placehold.co/320x180?text=Vacinação"
     alt="Cartão digital de vacinação vet com layout clean, ícones coloridos estilo material" />
```

---

## Segurança & Privacidade

- Tokens via Cookie seguro/application storage
- Inputs sanizados e validados
- Rotas de API protegidas, arquitetura ready para https
- Estrutura preparada para push notifications e biometria (futuras releases, plugins Cordova)

---
## Licença

Distribuído sob Licença MIT, para fins educacionais, institucionais, clínicos e comunitários.

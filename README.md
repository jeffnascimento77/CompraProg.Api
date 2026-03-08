# Sistema de Compra Programada de Ações

## Visão Geral

Este projeto implementa um **protótipo de um sistema de compra programada de ações**, inspirado em produtos oferecidos por corretoras de investimento.

A ideia central é permitir que diversos clientes invistam mensalmente em uma **carteira recomendada de ações**, realizando compras automáticas em datas pré-definidas e distribuindo os ativos proporcionalmente entre os participantes.

O sistema simula várias regras de negócio presentes em plataformas financeiras, como:

* compras consolidadas
* distribuição proporcional de ativos
* controle de custódia
* cálculo de preço médio
* geração de eventos fiscais
* integração com mensageria (Kafka)

Este projeto foi desenvolvido como um **exercício técnico de arquitetura e regras de negócio**, focando na clareza da implementação e na separação de responsabilidades.

---

# Tecnologias Utilizadas

O projeto foi desenvolvido utilizando as seguintes tecnologias:

* **.NET 8**
* **ASP.NET Core Web API**
* **Entity Framework Core**
* **MySQL**
* **Apache Kafka**
* **Docker**
* **Swagger / OpenAPI**

Essas tecnologias foram escolhidas por serem amplamente utilizadas em sistemas corporativos e por oferecerem boa integração entre si.

---

# Arquitetura Utilizada

O sistema foi estruturado utilizando uma **arquitetura em camadas**, com separação clara de responsabilidades entre os componentes.

Estrutura do projeto:

```
src
│
├─ CompraProg.Api
│   Controllers
│   Program.cs
│
├─ CompraProg.Application
│   Helpers
│   Regras auxiliares
│
├─ CompraProg.Domain
│   Entidades de domínio
│
└─ CompraProg.Infrastructure
    Persistence
    Services
    Messaging
```

### Responsabilidade de cada camada

**Api**

Responsável por expor os endpoints HTTP e receber requisições externas.

Contém:

* Controllers
* configuração da aplicação
* integração com Swagger

---

**Application**

Contém regras auxiliares de negócio e cálculos reutilizáveis, como:

* cálculo de preço médio
* distribuição proporcional
* utilitários do motor

---

**Domain**

Representa os conceitos principais do sistema.

Inclui entidades como:

* Cliente
* Cesta de investimentos
* Itens da cesta

---

**Infrastructure**

Camada responsável por integração com recursos externos:

* banco de dados (Entity Framework)
* Kafka
* serviços de custódia
* persistência

---

# Decisões Técnicas

## 1. Separação em Camadas

A arquitetura em camadas foi adotada para facilitar:

* manutenção do código
* evolução do sistema
* testes
* reutilização de regras de negócio

Essa separação também evita que regras importantes fiquem diretamente dentro de controllers.

---

## 2. Uso do Entity Framework Core

O EF Core foi escolhido para simplificar:

* acesso ao banco
* mapeamento objeto-relacional
* migrations de banco de dados

Isso permite evoluir o schema do banco de forma controlada.

---

## 3. Banco de Dados MySQL

O MySQL foi escolhido por:

* simplicidade de configuração
* ampla adoção em aplicações corporativas
* boa integração com Docker

---

## 4. Uso de Kafka para eventos fiscais

O sistema publica eventos fiscais no Kafka para simular integração com outros serviços.

Eventos publicados incluem:

* IR dedo-duro de operações de compra

Esse padrão segue uma arquitetura orientada a eventos.

---

## 5. Importação de Cotações via COTAHIST

Os preços das ações são importados a partir do arquivo oficial da B3:

```
COTAHIST
```

Esse arquivo possui layout posicional fixo.

Durante a importação o sistema:

* lê cada linha
* extrai os campos necessários
* converte valores
* grava no banco

Apenas mercados relevantes são importados.

---

# Funcionalidades Implementadas

Até o momento o sistema possui as seguintes funcionalidades.

---

## Cadastro de Clientes

Permite que um cliente adira ao produto de compra programada.

Endpoint:

```
POST /api/clientes/adesao
```

Dados solicitados:

* Nome
* CPF
* Email
* Valor mensal de investimento

Regras aplicadas:

* CPF deve ser único
* valor mínimo de investimento validado
* cliente inicia ativo

Durante a adesão também é criada automaticamente uma **custódia filhote**.

---

# Cesta de Investimento (Top Five)

Existe uma carteira recomendada composta por 5 ações.

Endpoint administrativo:

```
POST /api/admin/cesta
```

Regras:

* exatamente 5 ativos
* soma dos percentuais = 100%
* apenas uma cesta ativa por vez

Quando uma nova cesta é criada, a anterior é automaticamente desativada.

---

# Importação de Cotações

Endpoint:

```
POST /api/cotacoes/importar-cotahist
```

O endpoint recebe um arquivo TXT do COTAHIST e realiza:

* leitura das linhas
* parsing do layout
* extração de ticker e preço de fechamento

persistência no banco

Durante a importação são contabilizados:

linhas lidas

linhas parseadas

linhas inseridas

linhas ignoradas

Motor de Compra Programada

O motor executa as compras consolidadas.

Endpoint:

POST /api/motor/executar-compra

Fluxo executado:

busca clientes ativos

calcula valor consolidado de investimento

calcula quantas ações comprar de cada ativo

verifica saldo na custódia master

determina quantidade real a comprar

Estrutura de Custódia

O sistema implementa dois tipos de custódia.

Custódia Master

Responsável por:

centralizar as compras

armazenar resíduos

Custódia Filhote

Cada cliente possui sua própria custódia.

Ela contém:

posições por ativo

quantidade

preço médio

Distribuição Proporcional

Após a compra consolidada, os ativos são distribuídos proporcionalmente entre os clientes.

Cálculo utilizado:

quantidadeCliente = floor(quantidadeTotal * proporcaoCliente)

As sobras são mantidas na custódia master como resíduos.

Cálculo de Preço Médio

O preço médio é atualizado somente em compras.

Fórmula utilizada:

PM = (QtdAnterior * PMAnterior + QtdNova * PrecoCompra) / (QtdAnterior + QtdNova)

Esse valor é mantido por ativo em cada custódia filhote.

Integração com Kafka

Após cada distribuição de ativo, o sistema publica um evento no Kafka.

Estrutura do evento:

clienteId

cpf

ticker

quantidade

preço unitário

valor da operação

valor do imposto

Esse evento simula o envio de informações fiscais para outro sistema.

Como Rodar o Projeto
1. Clonar o repositório
git clone https://github.com/seu-usuario/compra-programada-acoes.git
2. Subir o banco de dados

Exemplo com Docker:

docker run -p 3306:3306 -e MYSQL_ROOT_PASSWORD=root mysql
3. Subir o Kafka

Exemplo simplificado:

docker run -p 9092:9092 apache/kafka
4. Executar a aplicação

Abrir a solução no Visual Studio e executar:

Start Debugging
5. Acessar Swagger

Após iniciar a aplicação:

https://localhost:xxxx/swagger

No Swagger é possível:

cadastrar clientes

criar cesta

importar cotação

executar motor

Próximos Passos do Projeto

Algumas funcionalidades ainda estão planejadas:

rebalanceamento da carteira quando a cesta mudar

IR sobre vendas

tela de rentabilidade

histórico completo de operações

controle de execução duplicada do motor

Considerações Finais

O sistema foi desenvolvido de forma incremental, priorizando:

clareza nas regras de negócio

separação de responsabilidades

facilidade de evolução

A arquitetura escolhida permite expandir o projeto com novos serviços ou integrações de forma organizada.

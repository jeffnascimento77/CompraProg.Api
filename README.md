# Sistema de Compra Programada de Ações

Este projeto implementa um **protótipo de um sistema de compra programada de ações**, inspirado em produtos reais de corretoras.
O objetivo é simular como um grupo de clientes pode investir mensalmente em uma carteira recomendada (Top Five), realizando compras automáticas, distribuindo os ativos proporcionalmente e mantendo controle de custódia, preço médio e eventos fiscais.

O projeto foi desenvolvido utilizando **.NET**, **MySQL**, **Kafka** e **Docker**, seguindo boas práticas de organização de código, separação de responsabilidades e arquitetura em camadas.

---

# Objetivo do Projeto

O sistema permite que vários clientes realizem **aportes mensais programados** em uma carteira recomendada de ações.

O funcionamento básico é:

1. Clientes aderem ao produto informando seus dados e valor de aporte mensal.
2. Existe uma **cesta de ações recomendadas (Top Five)** com percentuais definidos.
3. Em datas específicas do mês, o sistema executa automaticamente uma compra consolidada.
4. As ações compradas são distribuídas proporcionalmente entre os clientes.
5. Sobras (resíduos) ficam armazenadas em uma custódia master para uso em compras futuras.
6. O sistema mantém o **preço médio por ativo por cliente**.
7. Eventos fiscais são publicados em **Kafka** para processamento posterior.

---

# Tecnologias Utilizadas

* **.NET 8**
* **ASP.NET Web API**
* **Entity Framework Core**
* **MySQL**
* **Kafka**
* **Docker**
* **Swagger (OpenAPI)**

---

# Estrutura do Projeto

O projeto foi organizado em uma arquitetura em camadas para manter o código limpo e de fácil manutenção.

```
src
│
├─ CompraProg.Api
│   Controllers
│   Program.cs
│
├─ CompraProg.Application
│   Helpers
│
├─ CompraProg.Domain
│   Entidades de domínio
│
└─ CompraProg.Infrastructure
    Persistence
    Services
    Messaging
```

Cada camada possui uma responsabilidade clara:

| Camada         | Responsabilidade                    |
| -------------- | ----------------------------------- |
| Api            | Exposição dos endpoints HTTP        |
| Application    | Regras auxiliares e cálculos        |
| Domain         | Modelos de negócio                  |
| Infrastructure | Banco de dados, Kafka e integrações |

---

# Etapas Implementadas Até Agora

## 1. Estrutura Inicial da API

Foi criada uma API utilizando **ASP.NET Core**, com suporte a Swagger para facilitar os testes.

O projeto foi dividido em camadas para seguir boas práticas de arquitetura.

---

# 2. Cadastro de Clientes

Foi criado um endpoint para permitir que um cliente entre no sistema.

### Endpoint

```
POST /api/clientes/adesao
```

### Dados necessários

* Nome
* CPF
* Email
* Valor mensal de aporte

Regras aplicadas:

* CPF deve ser único
* Valor mínimo de investimento validado
* Cliente inicia com status ativo

Durante a adesão também é criada automaticamente uma **custódia filhote** para o cliente.

---

# 3. Cesta de Investimento (Top Five)

Foi implementado um endpoint administrativo para cadastrar a carteira recomendada.

### Endpoint

```
POST /api/admin/cesta
```

A cesta precisa conter:

* exatamente **5 ativos**
* percentuais que somem **100%**

Quando uma nova

# CommandEditor

A ideia do programa é automatizar a geração de código, por base em um json com as informações básicas o sistema gera os arquivos de acorco com o que está configurado nas pastas.

## Exemplo

A baixo segue um exemplo de Json que ira gerar os arquivos

```
{
    "pathIn": ".........ProjectPath.........\\Example\\In",
    "pathOut": ".........ProjectPath.........\\Example\\Out",
    "data": {
        "file": [{
                "nome": "teste1",
                "nomeId": "id",
                "tipoId": "uniqueidentifier",
                "campos": { "nome": "NomeStatus", "tipo": "[nvarchar](30)" }
            },
            {
                "nome": "teste2",
                "nomeId": "id",
                "tipoId": "uniqueidentifier",
                "campos": [
                    { "nome": "NomeStatus", "tipo": "[nvarchar](30)" },
                    { "nome": "NomeStatus", "tipo": "[nvarchar](30)" },
                    { "nome": "NomeStatus", "tipo": "[nvarchar](30)" }
                ]
            }
        ]
    }
}
```

Os 3 primeiros campos são obrigatórios, eles descrevem o caminho onde será lido os aquivos de exemplo (pathIn), o caminho onde serão colocado os arquivos de destino(pathOut), e os dados que serão substituidos(data).

Dentro do campo data, os objetos representam a rais dos daquivos que seram procurados na pathIn, no exemplo, qualquer arquivo com com a extenção *.file será criado no pathOut respeitando as pastas, esse objeto precisa ter ou um objeto ou uma lista dentro dele, no caso de lista ele executara para todos os arquivos cada objeto dentro da lista, e para um objeto passadado como parametro ou para cada objeto da lista ele busca a tag dentro do arquivo em chaves duplas ( {{ }} ) e subistitui pelo valor, no caso de um campo ser um objeto ou uma lista, ele ira procurar o arquivo que representa aquele objeto ou lista, no caso do exemplo temos a lista "campos", então ele ira procurar o arquivo .file.campos, e para cada tag dentro dele ele ira alterar o arquivo adicionando cada texto em uma linha e susbistituindo tudo no arquivo principal.

Caso o arquvio já exista na pasta de destino, ele irá procurar a tag com nome dos dados, no exemplo será {{file}}, se ele achar essa tag ele ira adicionar o conteudo do arquivo abaixo dessa linha, caso não ache o arquivo, ele ira adicionar o conteudo abaixo de todo o resto do conteudo.
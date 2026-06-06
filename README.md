<!-- markdownlint-disable-file MD033 -->

# Projeto de SRJ - Card Wars - Arthur Santos a22503968

Este relatório vai servir como um documentario do processo de criação do jogo, focando-se principalmente na parte da rede do jogo.

## Sobre o jogo

O jogo é bastante inspirado no jogo "Cards Wars" publicado pela Cartoon Network, foi lançado em 2014. Foi retirado das lojas em dezembro de 2019.

![CardWarsLogo](Images/CardWarsLogo.png)

<p align="center">
  <img src="Images/CardsWarGif_InitialSection.gif" alt="CardsWarGif_InitialSection" width="400">
</p>

## Design

O design do jogo é simples e consiste em:

- 2 jogadores um contra o outro.
- Cada jogador inicia com:
  - 20 HP
  - 4 cartas na mão
  - 4 cartas no baralho
  - 2 Mana
- Cada jogador possui 4 terrenos
  - Ao selecionar uma carta e um terreno, o jogador coloca a carta no terreno.
  - Cada terreno apenas pode ter 1 unidade.
- Cada carta custa X de mana
  - Uma carta pode custar entre 1 e 3 de mana.
  - Cada carta possui os seguintes atributos:
    - HP
    - Ataque
    - Custo
    - Efeito especial
- O jogo funciona por Rounds e Turnos:
  - Um Round consiste em 1 turno para cada jogador em um ciclo de:
    - Turno Player 1 -> Turno Player 2 -> Batalha -> Fim do Round
  - O turno inicia com o jogador 1.
  - Durante o seu turno, o jogador pode jogar cartas, visualizar cartas e terminar o seu turno.
  - Durante o turno do oponente, o jogador pode apenas visualizar as suas cartas enquanto espera que o turno termine.
  - No fim de ambos os turnos, as unidades causam o respetivo dano à unidade inimiga que está na mesma linha (com exceção de uma carta com o efeito especial de atacar todas as unidades).
  - No caso de não existirem unidades na linha, a unidade causa dano diretamente ao jogador inimigo, diminuindo o seu HP.
  - No início de um novo Round, ambos os jogadores recebem 2 de mana e X cartas até completarem 4 cartas na mão.

Aqui segue uma tabela de todas unidades com suas informações.

<p align="center">
  <img src="Images/UnitsTable.png" alt="CardsWarGif_InitialSection" width="600">
</p>

## Inicio do projeto

No início, apenas copiei o projeto "Wyzards" disponibilizado aos alunos. Usei-o como base para a parte de configuração da rede e para aproveitar as ferramentas bastante úteis.

Logo após isso, testei alguns sistemas básicos para validar a sincronização de rede entre os 2 jogadores e o servidor.

## Sistema de Grid

<p align="center">
  <img src="Images/GridAnimation.gif" alt="GridAnimation" width="600">
</p>

A primeira coisa que implementei foi este feedback visual, utilizando um código simples que ativa uma animação quando o rato está por cima do tile.

```c#
public sealed class GridTile : NetworkBehaviour
{
    private Animator _fillAnimator;
    private static readonly int ActiveHash = Animator.StringToHash("Active");

    private void Awake()
    {
        _fillAnimator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
    }

    public void SetHoverActive(bool state)
    {
        if (!IsClient) return;

        _fillAnimator?.SetBool(ActiveHash, state);
    }
}
```

O sistema utilizado para definir qual tile pertence a qual jogador foi mais complicado de implementar. Acho que, devido à minha inexperiência em programação de jogos online, acabei por pensar demasiado em sistemas muito complexos. No final, a solução revelou-se bastante simples e nem sequer precisou de lógica de rede. Com certeza esta solução tem limitações, mas para o âmbito do projeto funciona.

Basicamente, a forma como pensei implementar o sistema de "Tile Owning" foi atribuir um ID a cada jogador quando este é instanciado no jogo. No script `NetworkSetup.cs` é chamado um método do `PlayerController.cs` que define esse ID.

```c#
// Server assigns the player index
spawnedObject.SetPlayerIndex(playerPrefabIndex);

playerPrefabIndex = (playerPrefabIndex + 1) % playerPrefabs.Count;
```

Esse ID é depois utilizado para guardar, no script individual de cada tile (`GridTile.cs`), a indicação de qual jogador é o seu dono.

```c#
public int OwnerID { get; private set; }
```

Essa atribuição é feita através do script `GridManager.cs`, que percorre todos os tiles no método `Start()` e atribui um proprietário a cada um deles.

```c#
public class GridManager : NetworkBehaviour
{
    [SerializeField] private GridTile[] allTiles;

    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < allTiles.Length; i++)
            allTiles[i].SetOwner(i < 4 ? 0 : 1);
    }
}
```

No controlador do jogador é tratado o comportamento de "o rato estar por cima do tile". O processo é simples: é feito um raycast e, em seguida, é verificado se o proprietário do tile corresponde ao jogador atual.

```c#
    private void HandleTileInteraction()
    {
        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        GridTile targetedTile = null;

        if (hit.collider != null)
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();

            if (tile != null && tile.OwnerID == _playerIndex.Value)
                targetedTile = tile;
        }

        if (targetedTile == currentHoveredTile) return;

        currentHoveredTile?.SetHoverActive(false);
        currentHoveredTile = targetedTile;
        currentHoveredTile?.SetHoverActive(true);
    }
```

## Sistema de Cartas

<p align="center">
  <img src="Images/Cards.gif" alt="GridAnimation" width="600">
</p>

Este tópico vai ser dividido em algumas partes:

- Definição de Cartas
- Cartas para Cliente vs Servidor
- Instanciar Cartas
- Mostrar as Cartas

### Definição de Cartas

No jogo, uma carta é uma caixa de informação bruta que é distribuída para cada instância.

Existem 2 Scriptable Objects principais:

- `Card.cs`: Guarda toda a informação de uma carta.
- `PlayableCards.cs`: Guarda a informação de todas as cartas jogáveis.

Ao criar um `PlayableCardsAsset`, é necessário inserir todas as cartas jogáveis (`CardAsset`) na lista.

### Cartas para Cliente vs Servidor

As cartas são tratadas de formas diferentes entre o cliente e o servidor.

**Para o cliente**, as cartas são meramente contentores de informação visual. Os jogadores apenas notificam o servidor quando pretendem realizar algum tipo de ação com uma carta, seja jogá-la ou atualizar o estado da sua mão. O cliente apenas tem acesso direto às animações da carta.

**Para o servidor**, é sua responsabilidade distribuir as cartas para cada jogador e sincronizar o estado atual das mãos dos jogadores.

Este tipo de sistema é importante para evitar possíveis cheats por parte dos jogadores, como pedidos indevidos de cartas, alteração de estados de cartas, entre outros.

### Instanciar Cartas

As cartas são instanciadas no `GameManager.cs`.

Primeiro é feita a verificação de que o jogador existe no servidor.

```c#
    ulong clientId = rpcParams.Receive.SenderClientId;

    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
```

Depois, procura-se a instância do jogador e é executado todo o processo de seleção aleatória de 8 cartas, colocando 4 na mão do jogador e 4 no baralho.

Cada carta possui um ID único de instância, tornando possível existirem duas cartas iguais com IDs diferentes. Isto é feito através do script `CardInstance.cs`.

```c#
    PlayerController player = networkClient.PlayerObject.GetComponent<PlayerControlle();
    if (player != null)
    {
        player.deckState.Deck.Clear();
        player.deckState.Hand.Clear()
        for (int i = 0; i < 8; i++)
        {
            int idx = Random.Range(0, data.Cards.Count);
            CardInstance card = new CardInstance(cardsInGame++, data.Cards[idx].CardId(int)clientId);
            player.deckState.Deck.Enqueue(card);
        }
        
        // Draw 4 cards into the hand
        for (int i = 0; i < 4; i++)
        {
            player.deckState.Hand.Add(player.deckState.Deck.Dequeue());
        }

        for (int i = 0; i < player.deckState.Hand.Count; i++)
        {
            handInstanceIds[i] = player.deckState.Hand[i].InstanceId;
            handCardIds[i] = player.deckState.Hand[i].CardId;
        }
        
        CardInstance[] deckArray = player.deckState.Deck.ToArray();
        for (int i = 0; i < deckArray.Length; i++)
        {
            deckInstanceIds[i] = deckArray[i].InstanceId;
            deckCardIds[i] = deckArray[i].CardId;
        }
        
        // Only the client who requested the deck receives this network data
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
        };
    }
```

Após este processo, as cartas já pertencem ao jogador, mas ainda não existe qualquer feedback visual.

### Mostrar as Cartas

Para mostrar visualmente as cartas ao jogador, é chamado um método `ClientRpc` que sincroniza o estado da mão com a interface do jogo: `SyncDeckToClientClientRpc()`.

Não foi mostrado anteriormente, mas durante o processo de geração da mão e do baralho do jogador, cada ID é guardado para ser enviado ao método de sincronização. Desta forma, o baralho e a mão são apagados e reconstruídos novamente, mas agora com a inicialização da interface visual de cada carta.

```c#
    [ClientRpc]
    public void SyncDeckToClientClientRpc(int[] handInstanceIds, int[] handCardIds, int[] deckInstanceIds, int[] deckCardIds, ClientRpcParams clientRpcParams = default)
    {
        // Reconstruct logical state
        deckState.Hand.Clear();
        deckState.Deck.Clear();

        for (int i = 0; i < handInstanceIds.Length; i++)
        {
            deckState.Hand.Add(new CardInstance(handInstanceIds[i], handCardIds[i], ID));
        }

        for (int i = 0; i < deckInstanceIds.Length; i++)
        {
            deckState.Deck.Enqueue(new CardInstance(deckInstanceIds[i], deckCardIds[i], ID));
        }

        // Clear current UI card elements from previous generation 
        if (handUiContainer != null)
        {
            foreach (Transform child in handUiContainer)
            {
                Destroy(child.gameObject);
            }

            // Instantiate visual UI Card prefabs
            foreach (CardInstance cardInstance in deckState.Hand)
            {
                // Instantiate into the canvas layout group container
                GameObject instantiatedCard = Instantiate(cardUiPrefab, handUiContainer);

                CardUI cardUiScript  = instantiatedCard.GetComponent<CardUI>();
                if (cardUiScript != null)
                {
                    Card cardData = GameManager.Instance.GetCardDefinition(cardInstance.CardId);

                    if (cardData != null)
                    {
                        cardUiScript.Setup(cardData, cardInstance);
                    }
                }
                else
                {
                    Debug.LogError($"[Client] Could not find card data for Card ID: {cardInstance.CardId}");
                }
            }
        }
    }
```

## Sistema de Mana

É necessario utilizar mana para jogar cartas, essa mana inicia com um valor de 1 e a cada turno aumenta +2.

Eu utilizo uma Network Variable para alcançar isso:

```c#
private readonly NetworkVariable<int> _currentMana = new(
    1,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
```

Dentro do controlador do player existe um metodo para alterar o valor da mana. Isso poderia causar problemas com jogadores tentando trapacear, mas é facilmente evitavel com uma verificação se quem está acessando o metodo é o servidor e se caso for o cliente, o metodo não faz nada.

```c#
public void ModifyManaServeAuthoritative(int amount)
{
    if (!IsServer) return; // <- Essa verificação garante que apenas o servidor pode acessar o metodo
    _currentMana.Value = Mathf.Max(0, _currentMana.Value + amount);
}
```

## Contagem de Turnos

Para a contagem de turnos é algo parecido com a mana. Existe uma Network Variable dentro do `GameManager.cs` onde seu `Delegate` `OnValueChanged` é ouvido pelo método `OnTurnNumberChanged` dentro do controlador do jogador.

```c#
GameManager.Instance.CurrentTurnNumber.OnValueChanged += OnTurnNumberChanged;
```

Toda vez que um jogador termina seu turno, é feito um pedido de terminar o turno para o servidor.

```c#
private void OnEndTurnButtonClicked()
{
    // Check if it is players turn (even though the button should be disabled)
    if (ID == GameManager.Instance.ActivePlayerIndex.Value)
    {
        // Disable end turn button
        UiManager.Instance.SetEndTurnButtonInteractable(false);
        // Request to end players turn
        GameManager.Instance.RequestEndTurnServerRpc();
    }
}
```

Então o servidor verifica se ambos jogadores já fizeram suas jogadas, se sim, o servidor aumenta seu valor de turno, notificando o metodo em `PlayerController.cs` para fazer mudança do contador de turno de cada jogador.

```c#
[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
public void RequestEndTurnServerRpc(RpcParams rpcParams = default)
{
    ulong clientId = rpcParams.Receive.SenderClientId;

    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
    {
        PlayerController player = networkClient.PlayerObject.GetComponent<PlayerController>();

        if (player != null && player.ID == ActivePlayerIndex.Value)
        {
            playerFinishedThisRound++;

            if (playerFinishedThisRound >= 2)
            {
                // ...
                CurrentTurnNumber.Value++;
                // ...
            }
        }
    }
}
```

```c#
// Em PlayerController.cs
private void OnTurnNumberChanged(int previous, int current)
{
    if (IsOwner && UiManager.Instance != null)
    {
        UiManager.Instance.UpdateTurnText(current);
    }
}
```

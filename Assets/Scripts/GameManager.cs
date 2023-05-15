using System;
using System.Collections.Generic;
using System.Linq;
using LitJson;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public List<GameObject> pokerPrefab;
    [Header("卡背图案")] public GameObject cardBackPrefab;

    public GameObject menuPanel;
    public Text messageText;
    public GameObject roomPanel;
    public GameObject startGameButton;
    public GameObject leftPlayer;
    public GameObject rightPlayer;
    public GameObject topPlayer;
    public GameObject downPlayer;

    private GameObject empty;
    private GameObject _player;
    private String _roomName;
    private int _playerCount; //玩家在线上游戏时房间中的位置 (The player's position in the room when playing online)

    private bool _online; //是否为线上游戏 (Whether it is an online game)
    private bool roomOwner;

    private static GameManager _instance;
    private List<GameObject> _playerPrefabList = new List<GameObject>();
    private List<PlayerAble> _playerList = new List<PlayerAble>();
    private List<GameObject> dealCardList = new List<GameObject>();
    private int _passCount;

    //最后出的牌 (The last card played)
    private List<GameObject> lastPlay = new List<GameObject>();

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        _playerPrefabList.Add(downPlayer);
        _playerPrefabList.Add(rightPlayer);
        _playerPrefabList.Add(topPlayer);
        _playerPrefabList.Add(leftPlayer);
        empty = new GameObject("Player");
    }

    public void RestartGame()
    {
        if (_instance._online)
        {
            if (_instance.roomOwner)
                StartOnlineGame();
            else
                ShowText("Only the host can restart the game");
                //只有房主可以重新开始
        }
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnTitle()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /**
     * 刷新房间玩家状态 (Refresh room player status)
     */
    public static void RefreshRoom(WebSocketManeger.Room room, int index)
    {
        _instance.roomOwner = index == 0;
        _instance._roomName = room.name;
        _instance._online = true;

        for (var i = 0; i < _instance.empty.transform.childCount; i++)
        {
            Destroy(_instance.empty.transform.GetChild(i).gameObject);
        }

        _instance._playerCount = room.playerCount;
        var playerCount = _instance._playerCount;
        List<GameObject> playerList = new List<GameObject>();
        _instance._player = Instantiate(_instance.downPlayer, _instance.transform.position, Quaternion.identity);
        switch (index)
        {
            case 0:
                _instance.startGameButton.SetActive(true);
                playerList.Add(_instance._player);
                if (playerCount > 1)
                    playerList.Add(
                        Instantiate(_instance.rightPlayer, _instance.transform.position, Quaternion.identity));
                ;
                if (playerCount > 2)
                    playerList.Add(Instantiate(_instance.topPlayer, _instance.transform.position, Quaternion.identity));
                if (playerCount > 3)
                    playerList.Add(Instantiate(_instance.leftPlayer, _instance.transform.position,
                        Quaternion.identity));
                break;
            case 1:
                playerList.Add(Instantiate(_instance.leftPlayer, _instance.transform.position, Quaternion.identity));
                playerList.Add(_instance._player);
                if (playerCount > 2)
                    playerList.Add(
                        Instantiate(_instance.rightPlayer, _instance.transform.position, Quaternion.identity));
                if (playerCount > 3)
                    playerList.Add(Instantiate(_instance.topPlayer, _instance.transform.position, Quaternion.identity));

                break;
            case 2:
                playerList.Add(Instantiate(_instance.topPlayer, _instance.transform.position, Quaternion.identity));
                playerList.Add(Instantiate(_instance.leftPlayer, _instance.transform.position, Quaternion.identity));
                playerList.Add(_instance._player);
                if (playerCount > 3)
                    playerList.Add(
                        Instantiate(_instance.rightPlayer, _instance.transform.position, Quaternion.identity));
                break;
            case 3:
                playerList.Add(Instantiate(_instance.rightPlayer, _instance.transform.position, Quaternion.identity));
                playerList.Add(Instantiate(_instance.topPlayer, _instance.transform.position, Quaternion.identity));
                playerList.Add(Instantiate(_instance.leftPlayer, _instance.transform.position, Quaternion.identity));
                playerList.Add(_instance._player);
                break;
        }


        // _instance._index = index;

        playerList.ForEach(p => p.transform.SetParent(_instance.empty.transform));

        for (var i = 0; i < room.playerNameList.Count; i++)
        {
            var player = playerList[i].GetComponent<PlayerAble>();
            player.SetUsername(room.playerNameList[i]);
            player.SetIndex(i);
        }

        _instance._playerList = playerList.ConvertAll(player => player.GetComponent<PlayerAble>());


        if (playerCount.Equals(4) && 0.Equals(index))
            _instance.StartOnlineGame();
    }

    public void StartGame()
    {
        _instance._online = false;
        dealCardList = pokerPrefab;

        foreach (var playerPrefab in _playerPrefabList)
        {
            var player = Instantiate(playerPrefab, transform.position, Quaternion.identity);
            var playerAble = player.GetComponent<PlayerAble>();
            var index = _playerPrefabList.IndexOf(playerPrefab);
            playerAble.SetIndex(_playerPrefabList.IndexOf(playerPrefab));
            playerAble.SetUsername("AI" + index);
            if (index == 0)
                playerAble.SetUsername("You");
            _playerList.Add(playerAble);
            dealCardList = playerAble.SetCard(dealCardList);
        }
    }

    public void StartOnlineGame()
    {
        startGameButton.SetActive(false);
        var pokerList = pokerPrefab.ConvertAll(card => card.GetComponent<PokerController>().poker);
        //如果是第一个玩家 负责发牌 (If it's the first player, it's responsible for dealing cards)

        for (var i = 0; i < _playerPrefabList.Count; i++)
        {
            List<PokerController.Poker> playerHandCardList = new List<PokerController.Poker>();
            while (playerHandCardList.Count < 13)
            {
                var cardIndex = Random.Range(0, pokerList.Count - 1);
                playerHandCardList.Add(pokerList[cardIndex]);
                pokerList.RemoveAt(cardIndex);
            }

            //告诉服务器这位玩家手里有什么牌 (Tell the server what cards this player has in his hand)
            WebSocketManeger.SetHandCard(i, JsonMapper.ToJson(playerHandCardList), _roomName);
        }

        //告诉服务器该房间开始游戏 (Tell the server that the room is starting the game)
        WebSocketManeger.StartGame(_roomName);
    }

    /**
     * 将 pokercontroller list 与prefab一一对应再传给playerController 
     * (One-to-one correspondence between pokercontroller list and prefab and then pass it to playercontroller)
     * 好像很费性能? 但是暂时没好方法 (It seems to be very expensive? But there is no good way for the time being)
     */
    public static void SetHandCard(List<PokerController.Poker> pokerList)
    {
        CleanDropZone();
        List<GameObject> pokerGameObjectList = new List<GameObject>();
        foreach (var poker in pokerList)
        {
            foreach (var gameObject in _instance.pokerPrefab)
            {
                var prefabController = gameObject.GetComponent<PokerController>();
                if (prefabController.poker.color.Equals(poker.color)
                    && prefabController.poker.point.Equals(poker.point))
                {
                    pokerGameObjectList.Add(Instantiate(gameObject, _instance.transform.position,
                        Quaternion.identity));
                }
            }
        }

        _instance._player.GetComponent<PlayerController>().RefreshCard(pokerGameObjectList);
    }

    /**
     * 出牌 (Play card)
     */
    public static void PlayCard(int index, List<GameObject> handCard, bool firstTurn = false)
    {
        if (firstTurn && !ContainsCube3(handCard))
            return;
        if (!_instance.ValidPlay(handCard))
            return;

        List<GameObject> playList = handCard.FindAll(card => card.GetComponent<PokerController>().@select);

        //将打出的牌从手牌中删除 (Remove the cards played from the hand)
        foreach (var card in playList)
        {
            handCard.Remove(card);
        }


        //如果是线上游戏 告知服务器 (If it's an online game, tell the server)
        if (_instance._online)
        {
            playList.ForEach(Destroy);

            WebSocketManeger.PlayCard(index,
                JsonMapper.ToJson(playList.ConvertAll(card => card.GetComponent<PokerController>().poker)),
                _instance._roomName);

            if (handCard.Count == 0)
            {
                _instance._playerList[index].Win();
                return;
            }

            _instance._player.GetComponent<PlayerController>().DropTurn();
        }
        else
        {
            if (handCard.Count == 0)
            {
                _instance._playerList[index].Win();
                return;
            }

            CleanDropZone();
            NextTurn();
            PutCardToDropZone(index, playList);

            _instance._passCount = 0;
            _instance.lastPlay.Clear();
            _instance.lastPlay.AddRange(playList);
        }
    }

    /**
     * 将牌放到出牌区 (Put the card in the drop zone)
     */
    private static void PutCardToDropZone(int index, List<GameObject> playList)
    {
        //有玩家出牌 passCount清零 (If a player plays a card, passCount is cleared)
        _instance._passCount = 0;
        _instance._playerList[index].PutCardToDropZone(playList);
    }


    /**
     * 第一轮出牌必须包含方块3 (The first round of play must include cube 3)
     */
    private static bool ContainsCube3(List<GameObject> handCard)
    {
        foreach (var card in handCard)
        {
            if (card.GetComponent<PokerController>().poker.color == PokerController.Color.Diamonds
                && card.GetComponent<PokerController>().poker.point.Equals(3))
                return true;
        }

        return false;
    }

    /**
     * 出牌是否有效  (Is the play valid)
     *
     */
    public bool ValidPlay(List<GameObject> handCard)
    {
        bool flag = false;
        List<PokerController> selectedCardList = new List<PokerController>();
        foreach (var card in handCard)
        {
            if (card.GetComponent<PokerController>().select)
                selectedCardList.Add(card.GetComponent<PokerController>());
        }

        if (lastPlay.Count > 0 && selectedCardList.Count != lastPlay.Count)
            return false;

        //判断出牌是否合规 (Determine if the play is compliant)
        switch (selectedCardList.Count)
        {
            case 1:
                flag = true;
                break;
            case 2:
                flag = PlayPair(selectedCardList);
                break;
            case 3:
                flag = PlayThree(selectedCardList);
                break;
            case 5:
                flag = ValidPlay5Card(selectedCardList);
                break;
        }

        if (flag && lastPlay.Count > 0)
            flag = CompareCard(selectedCardList, lastPlay.ConvertAll(x => x.GetComponent<PokerController>()));

        // return false;
        return flag;
    }

    /**
     * 比大小 (Compare size)
     */
    private bool CompareCard(List<PokerController> selectedCardList, List<PokerController> lastPlayList)
    {
        var flag = false;
        switch (selectedCardList.Count)
        {
            case 1:
                flag = CompareSingle(selectedCardList[0], lastPlayList[0]);
                break;
            case 2:
                flag = ComparePair(selectedCardList, lastPlayList);
                break;
            case 3:
                flag = CompareThree(selectedCardList, lastPlayList);
                break;
            case 5:
                flag = CompareFive(selectedCardList, lastPlayList);
                break;
        }

        return flag;
    }

    /**
     * 比5张牌 (最麻烦) (Compare 5 cards (the most troublesome))
     * 顺子<花色<3+2<4+1<同花顺 (Straight flush < color < 3+2 < 4+1 < straight flush)
     */
    private bool CompareFive(List<PokerController> selectedCardList, List<PokerController> lastPlayList)
    {
        var selectedCardType = CheckCardType(selectedCardList);
        var lastPlayCardType = CheckCardType(lastPlayList);
        if (selectedCardType > lastPlayCardType)
            return true;
        bool flag = false;
        switch (lastPlayCardType)
        {
            case PokerController.CardType.蛇:
            case PokerController.CardType.同花:
            case PokerController.CardType.同花顺:
                //比最大那张牌 (Compare the largest card)
                var selectedCardMax = selectedCardList.Max();
                var lastPlayCardMax = lastPlayList.Max();
                flag = CompareSingle(selectedCardMax, lastPlayCardMax);
                break;
            case PokerController.CardType.葫芦:
                //比 同样那3张牌的点数 (Compare the same 3 cards)
                var selectedCard = selectedCardList.GroupBy(x => x.poker.point)
                    .Select(card => new {key = card.Key, total = card.Count()})
                    .ToDictionary(x => x.total, x => x.key);

                var lastPlayCard = lastPlayList.GroupBy(x => x.poker.point)
                    .Select(card => new {key = card.Key, total = card.Count()})
                    .ToDictionary(x => x.total, x => x.key);
                flag = selectedCard[3] > lastPlayCard[3];
                break;
            case PokerController.CardType.金刚:
                //比相同那4张牌的点数 (Compare the same 4 cards)
                selectedCard = selectedCardList.GroupBy(x => x.poker.point)
                    .Select(card => new {key = card.Key, total = card.Count()})
                    .ToDictionary(x => x.total, x => x.key);

                lastPlayCard = lastPlayList.GroupBy(x => x.poker.point)
                    .Select(card => new {key = card.Key, total = card.Count()})
                    .ToDictionary(x => x.total, x => x.key);
                flag = selectedCard[4] > lastPlayCard[4];
                break;
        }

        return flag;
    }

    /**
     * 比干炒 (Compare dry fry)
     * 比点数  (Compare points)
     */
    private bool CompareThree(List<PokerController> selectedCardList, List<PokerController> lastPlayList)
    {
        var selectedCardPointSum = selectedCardList.ConvertAll(x => x.poker.point).Sum();
        var lastPlayListPointSum = lastPlayList.ConvertAll(x => x.poker.point).Sum();
        return selectedCardPointSum > lastPlayListPointSum;
    }

    /**
     * 比对子 (Compare pairs)
     * 先比点数大小 (Compare the size of the points first)
     * 点数一样比花色 (If the points are the same, compare the color)
     * 有黑桃的大 (Big with spades)
     */
    private bool ComparePair(List<PokerController> selectedCardList, List<PokerController> lastPlayList)
    {
        var selectedCardPointSum = selectedCardList.ConvertAll(x => x.poker.point).Sum();
        var lastPlayListPointSum = lastPlayList.ConvertAll(x => x.poker.point).Sum();
        if (selectedCardPointSum > lastPlayListPointSum)
            return true;
        if (selectedCardPointSum.Equals(lastPlayListPointSum))
        {
            var colorList = selectedCardList.ConvertAll(x => x.poker.color);
            return colorList.Contains(PokerController.Color.Spade);
        }

        return false;
    }

    /**
     * 单张比大小 (Compare the size of a single card)
     * 先比点数大小 (Compare the size of the points first)
     * 点数一样比花色 (If the points are the same, compare the color)
     */
    private bool CompareSingle(PokerController selectedCard, PokerController lastPlayCard)
    {
        if (selectedCard.poker.point > lastPlayCard.poker.point)
            return true;
        if (selectedCard.poker.point == lastPlayCard.poker.point)
        {
            return selectedCard.poker.color > lastPlayCard.poker.color;
        }

        return false;
    }

    /**
     * 判断出对子 (Determine the pair)
     * 
     * 2张牌点数要一样 (2 cards must have the same points)
     */
    public bool PlayPair(List<PokerController> selectedCardList)
    {
        return selectedCardList[0].poker.point.Equals(selectedCardList[1].poker.point);
    }

    /**
     * 干炒 (Dry fry)
     * 判断出3张牌 (Determine 3 cards)
     * 3张牌点数要一样 (3 cards must have the same points)
     */
    public bool PlayThree(List<PokerController> selectedCardList)
    {
        return selectedCardList[0].poker.point.Equals(selectedCardList[1].poker.point)
               && selectedCardList[0].poker.point.Equals(selectedCardList[2].poker.point);
    }

    /**
     * 判断出5张牌 (Determine 5 cards)
     * 4+1
     * 3+2
     * 顺序
     * 同花
     * 同花顺
     */
    public bool ValidPlay5Card(List<PokerController> selectedCardList)
    {
        return
            PlayStraight(selectedCardList)
            || PlayFlush(selectedCardList)
            || PlayFullHouse(selectedCardList)
            || PlayFourAndOne(selectedCardList)
            || StraightFlush(selectedCardList)
            ;
    }

    public PokerController.CardType CheckCardType(List<PokerController> cardList)
    {
        if (PlayStraight(cardList))
            return PokerController.CardType.蛇;
        if (PlayFlush(cardList))
            return PokerController.CardType.同花;
        if (PlayFullHouse(cardList))
            return PokerController.CardType.葫芦;
        if (PlayFourAndOne(cardList))
            return PokerController.CardType.金刚;

        return PokerController.CardType.同花顺;
    }


    /**
     * 四條
     * 4个点数相同+1张单张 (4 points are the same + 1 single card)
     */
    private bool PlayFourAndOne(List<PokerController> selectedCardList)
    {
        Dictionary<int, int> pointCount = new Dictionary<int, int>();
        foreach (var pokerController in selectedCardList)
        {
            var point = pokerController.poker.point;
            if (pointCount.ContainsKey(point))
            {
                var value = pointCount[point];
                pointCount[point] = value + 1;
            }
            else
            {
                pointCount[point] = 1;
            }
        }

        var flag = pointCount.Count == 2 && pointCount.ContainsValue(4) && pointCount.ContainsValue(1);
        if (flag)
            Debug.Log("四条"); //TODO
        return flag;
    }

    /**
     * 同花顺 (Straight flush)
     */
    private bool StraightFlush(List<PokerController> selectedCardList)
    {
        return PlayStraight(selectedCardList) && PlayFlush(selectedCardList);
    }

    /**
     * 夫佬 (Full house)
     * 3张相同点数+2张相同点数 (3 cards with the same points + 2 cards with the same points)
     */
    private bool PlayFullHouse(List<PokerController> selectedCardList)
    {
        Dictionary<int, int> pointCount = new Dictionary<int, int>();
        foreach (var pokerController in selectedCardList)
        {
            var point = pokerController.poker.point;
            if (pointCount.ContainsKey(point))
            {
                var value = pointCount[point];
                pointCount[point] = value + 1;
            }
            else
            {
                pointCount[point] = 1;
            }
        }

        var flag = pointCount.Count == 2 && pointCount.ContainsValue(3) && pointCount.ContainsValue(2);
        if (flag)
            Debug.Log("夫佬"); //TODO
        return flag;
    }

    /**
     * 同花 (Flush)
     * 五张牌同花色 (Five cards of the same color)
     */
    private bool PlayFlush(List<PokerController> selectedCardList)
    {
        for (var i = 1; i < selectedCardList.Count; i++)
        {
            if (selectedCardList[i].poker.color != selectedCardList[i - 1].poker.color)
                return false;
        }

        Debug.Log("同花"); //TODO
        return true;
    }

    /**
     * 蛇 (Snake)
     * 五张顺序牌 (Five sequential cards)
     */
    private bool PlayStraight(List<PokerController> selectedCardList)
    {
        selectedCardList.Sort((x, y) => x.poker.point.CompareTo(y.poker.point));
        for (var i = 1; i < selectedCardList.Count; i++)
        {
            if (selectedCardList[i].poker.point - selectedCardList[i - 1].poker.point != 1)
                return false;
        }

        Debug.Log("蛇"); //TODO
        return true;
    }

    /**
     * 清理出牌堆 (Clean up the discard pile)
     */
    public static void CleanDropZone()
    {
        _instance._playerList.ForEach(player => player.CleanDropZone());
        _instance.lastPlay.Clear();
    }

    /**
     * 玩家PASS (Player PASS)
     */
    public static void Pass(int index)
    {
        //没人出牌不能pass (No one can pass without playing cards)
        if (_instance.lastPlay.Count == 0)
            return;

        if (_instance._online)
        {
            _instance._player.GetComponent<PlayerAble>().DropTurn();
            WebSocketManeger.Pass(index, _instance._roomName);
            return;
        }

        _instance._passCount++;
        //其他玩家都pass 就清空出牌区 (Other players pass and clear the discard area)
        if (_instance._passCount.Equals(_instance._playerList.Count - 1))
            CleanDropZone();
        NextTurn();
    }

    /**
     * 下一个玩家出牌 (The next player plays cards)
     */
    private static void NextTurn()
    {
        var playerList = _instance._playerList;
        for (var i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].GetTurn())
            {
                playerList[i].DropTurn();
                playerList[(i + 1) % playerList.Count].SetTurn();
                return;
            }
        }
    }

    /**
     * 获取最后出的牌 (Get the last card)
     */
    public static List<GameObject> GetLastPlay()
    {
        return _instance.lastPlay;
    }


    /**
     * 设置最后打出的牌 (Set the last card)
     */
    public static void SetLastPlay(int index, List<PokerController.Poker> playcardList)
    {
        CleanDropZone();
        _instance.lastPlay.Clear();
        foreach (var poker in playcardList)
        {
            foreach (var gameObject in _instance.pokerPrefab)
            {
                var prefabController = gameObject.GetComponent<PokerController>();
                if (prefabController.poker.color.Equals(poker.color)
                    && prefabController.poker.point.Equals(poker.point))
                {
                    _instance.lastPlay.Add(Instantiate(gameObject, _instance.transform.position,
                        Quaternion.identity));
                }
            }
        }

        PutCardToDropZone(index, _instance.lastPlay);
    }

    /**
     * 轮流出牌  (Take turns to play cards)
     * 当轮到自己 passCount刚好是玩家数-1 说明自己上次出的牌最大 (When it's your turn, passCount is just the number of players - 1, which means that the last card you played is the largest)
     * 清空出牌区 (Clear the discard area)
     */
    public static void SetTurn(int index)
    {
        var player = _instance._player.GetComponent<PlayerAble>();
        if (index.Equals(player.GetIndex()))
        {
            if (_instance._passCount >= (_instance._playerCount - 1))
            {
                WebSocketManeger.CleanDropZone(_instance._roomName);
            }

            _instance._player.GetComponent<PlayerController>().SetTurn();
        }
    }

    public static void ShowText(string message)
    {
        _instance.CancelInvoke("DisableShowText");
        _instance.messageText.gameObject.SetActive(true);
        _instance.messageText.text = message;
        _instance.Invoke("DisableShowText", 3f);
    }

    private void DisableShowText()
    {
        _instance.messageText.gameObject.SetActive(false);
    }

    //有玩家PASS (A player passes)
    public static void SetPass(int passIndex)
    {
        _instance._passCount++;
        var index = _instance._player.GetComponent<PlayerAble>().GetIndex();
        _instance._playerList[passIndex].ShowMessage("PASS");
        //如果是上家PASS ,轮到自己出牌 (If it's the last player to pass, it's your turn to play)
        if (((passIndex + 1) % _instance._playerCount).Equals(index))
        {
            SetTurn(index);
        }
    }

    public static void RefreshPlayer(List<WebSocketManeger.Player> playerList)
    {
        var index = _instance._player.GetComponent<PlayerAble>().GetIndex();
        for (var i = 0; i < playerList.Count; i++)
        {
            if (index.Equals(i))
                continue;

            var realHandCardCount = playerList[i].poker.Count;
            var player = _instance._playerList[i];
            var currentHandCardCount = player.GetHandCard().Count;
            if (realHandCardCount.Equals(currentHandCardCount))
                continue;
            player.ClearHandZone();
            List<GameObject> cardBackList = new List<GameObject>();
            while (cardBackList.Count < realHandCardCount)
            {
                var cardBack = Instantiate(_instance.cardBackPrefab, _instance.transform.position,
                    player.handZone.transform.rotation);
                cardBackList.Add(cardBack);
            }

            player.SetHandZone(cardBackList);
        }
    }

    public static void ClosePanel()
    {
        _instance.roomPanel.SetActive(false);
    }

    public static void Win(string username)
    {
        if (_instance._online)
        {
            WebSocketManeger.Win(_instance._roomName, username);
            return;
        }

        ShowText("Winner is [" + username + "]");
        _instance._playerList.ForEach(player => player.enabled = false);
        _instance.menuPanel.SetActive(true);
    }

    public static void AnnounceWin(string message)
    {
        _instance.menuPanel.SetActive(true);
        ShowText(message);
    }
}
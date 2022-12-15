"use strict";

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

let gameSessionConnection = new signalR.HubConnectionBuilder()
    .withUrl("/gamesessionhub")
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

gameSessionConnection.on("UserHasJoinedTheSession", function (message) {
    $.notify(message, "success");
});

gameSessionConnection.on("UserHasLeftTheSession", function (message) {
    $.notify(message, "info");
});

gameSessionConnection.on("GameSessionHasBeenStarted", function (message) {
    $("#gameStatus").html(message);
});

gameSessionConnection.on("ScoreboardUpdated", function (scoreboard) {
    console.log(scoreboard);

    $("#avengers-scores").html(scoreboard.avengersOverallScore);
    $("#justiceleave-scores").html(scoreboard.justiceLeagueOverallScore);

    let avengersUsers = "";
    scoreboard.avengers.forEach(function (user, idx) {
        console.log(user);
        avengersUsers += `<tr>
                            <th scope="row">${(idx + 1)}</th>
                            <td>${user.name}</td>
                            <td>${user.highScore}</td>
                        </tr>`
    });
    $("#list-group-avengers").html(avengersUsers);

    let justiceleagueUsers = "";
    scoreboard.justiceLeague.forEach(function (user, idx) {
        console.log(user);
        justiceleagueUsers += `<tr>
                            <th scope="row">${(idx + 1)}</th>
                            <td>${user.name}</td>
                            <td>${user.highScore}</td>
                        </tr>`
    });
    $("#list-group-justiceleague").html(justiceleagueUsers);

});

gameSessionConnection.onreconnecting(error => {
    console.assert(gameSessionConnection.state === signalR.HubConnectionState.Reconnecting);
    console.log(error);
    $("#gameSessionConnectionStatus").html("Connection lost due to error. Reconnecting.");
});

gameSessionConnection.onreconnected(connectionId => {
    console.assert(gameSessionConnection.state === signalR.HubConnectionState.Connected);
    console.log(connectionId);
    $("#gameSessionConnectionStatus").html("Connection reestablished. Connected with connectionId");
});


async function start() {
    try {
        await gameSessionConnection.start();
        console.assert(gameSessionConnection.state === signalR.HubConnectionState.Connected);
        console.log("SignalR Connected.");
        $("#gameSessionConnectionStatus").html("Connected!");

        let currentSessionToken = getCookie("currentSessionToken");
        let currentUserToken = getCookie("currentUserToken");

        gameSessionConnection.invoke("AddUserToSession", currentSessionToken, currentUserToken).catch(function (err) {
            return console.error(err.toString());
        });
    } catch (err) {
        console.assert(connection.state === signalR.HubConnectionState.Disconnected);
        console.log(err);
        setTimeout(() => start(), 5000);
    }
};

start();

gameSessionConnection.onclose(error => {
    start();
    console.assert(gameSessionConnection.state === signalR.HubConnectionState.Disconnected);
    console.log(error);
    $("#gameSessionConnectionStatus").html("Connection closed due to error. Try refreshing this page to restart the connection.");
});


function SubmitUserScore(score) {
    if (score == 0 || !isTheGameStarted) return;

    let currentSessionToken = getCookie("currentSessionToken");
    let currentUserToken = getCookie("currentUserToken");

    let args = {
        SessionToken: currentSessionToken,
        UserAccountToken: currentUserToken,
        Score: score
    };

    gameSessionConnection.invoke("SaveUserScore", args).catch(function (err) {
        return console.error(err.toString());
    });
}
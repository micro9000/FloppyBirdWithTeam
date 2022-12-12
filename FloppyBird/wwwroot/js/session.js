"use strict";

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

var gameSessionConnection = new signalR.HubConnectionBuilder().withUrl("/gamesessionhub").build();

gameSessionConnection.on("UserHasJoinedTheSession", function (message) {
    $.notify(message, "success");
});

gameSessionConnection.on("ScoreboardUpdated", function (scoreboard) {
    console.log(scoreboard);
});

gameSessionConnection.start().then(function () {
    $("#gameSessionConnectionStatus").html("Game session connection started");

    var currentSessionToken = getCookie("currentSessionToken");
    var currentUserToken = getCookie("currentUserToken");

    gameSessionConnection.invoke("AddUserToSession", currentSessionToken, currentUserToken).catch(function (err) {
        return console.error(err.toString());
    });
}).catch(function (err) {
    return console.error(err.toString());
});

function SubmitUserScore(score) {
    if (score == 0) return;
    console.log("score:", score);

    var currentSessionToken = getCookie("currentSessionToken");
    var currentUserToken = getCookie("currentUserToken");

    var args = {
        SessionToken: currentSessionToken,
        UserAccountToken: currentUserToken,
        Score: score
    };

    gameSessionConnection.invoke("SaveUserScore", args).catch(function (err) {
        return console.error(err.toString());
    });
}
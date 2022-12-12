"use strict";

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

let gameSessionConnection = new signalR.HubConnectionBuilder().withUrl("/gamesessionhub").build();

gameSessionConnection.on("UserHasJoinedTheSession", function (message) {
    $.notify(message, "success");
});

gameSessionConnection.on("ScoreboardUpdated", function (scoreboard) {
    console.log(scoreboard);

    $("#avengers-scores").html(scoreboard.avengersOverallScore);
    $("#justiceleave-scores").html(scoreboard.justiceLeagueOverallScore);

    let avengersUsers = "";
    scoreboard.avengers.forEach(function (user, idx) {
        console.log(user);
        avengersUsers += `<li class="list-group-item d-flex justify-content-between align-items-center">
                            ${user.name}
                            <span class="badge bg-primary rounded-pill">${user.highScore}</span>
                        </li>`;
    });
    $("#list-group-avengers").html(avengersUsers);

    let justiceleagueUsers = "";
    scoreboard.justiceLeague.forEach(function (user, idx) {
        console.log(user);
        justiceleagueUsers += `<li class="list-group-item d-flex justify-content-between align-items-center">
                            ${user.name}
                            <span class="badge bg-primary rounded-pill">${user.highScore}</span>
                        </li>`;
    });
    $("#list-group-justiceleague").html(justiceleagueUsers);

});

gameSessionConnection.start().then(function () {
    $("#gameSessionConnectionStatus").html("Game session connection started");

    let currentSessionToken = getCookie("currentSessionToken");
    let currentUserToken = getCookie("currentUserToken");

    gameSessionConnection.invoke("AddUserToSession", currentSessionToken, currentUserToken).catch(function (err) {
        return console.error(err.toString());
    });
}).catch(function (err) {
    return console.error(err.toString());
});

function SubmitUserScore(score) {
    if (score == 0) return;
    console.log("score:", score);

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
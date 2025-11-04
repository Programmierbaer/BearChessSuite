$(document).ready(function () {

    var properties = ["pieceType", "fen", "imageFolder", "lightSquare", "darkSquare","font"];
    var booleans = ["autoFlip", "labels"];

    var els = $(".chess-fen-container");
    for (var i = 0, len = els.length; i < len; i++) {
        var elementConfig = {
            "pieceType": "alpha"
        };
        var el = $(els[i]);
        var attr, val;
        for (var j = 0; j < properties.length; j++) {

            attr = "data-" + properties[j];
            val = el.attr(attr);
            if (val) {
                elementConfig[properties[j]] = val;
            }
        }

        for (j = 0; j < booleans.length; j++) {

            attr = "data-" + booleans[j];
            val = el.attr(attr);
            if (val) {
                elementConfig[booleans[j]] = val == "1" || val == "true";
            }
        }


        elementConfig.squareSize = el.width() / 8.2;
        elementConfig.renderTo = el;
        new DG.ChessFen20(elementConfig);


    }
    console.log(els.length);
});

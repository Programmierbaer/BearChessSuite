/**
 * DHTMLGoodies.com, Alf Magne Kalleland, December, 11th, 2015
 */
if (!DG)var DG = {};

if(!DG.ChessFen20Config) {
    DG.ChessFen20Config = {
        imageFolder: "/Images/",
        darkSquares: "square-dark.png",
        lightSquares: "square-light.png"
    };
}


DG.ChessFen20 = function (config) {

    var globalConfig = DG.ChessFen20Config ? DG.ChessFen20Config : {};
    this.lightSquare = globalConfig.lightSquare || "square-light.png";
    this.darkSquare = globalConfig.lightSquare || "square-dark.png";
    this.imageFolder = globalConfig.imageFolder || "/Images/";


    var properties = ["fen", "labels", "autoFlip", "font", "imageFolder", "lightSquare", "darkSquare", "labelFontSize"];
    if (config.renderTo != undefined)this.renderTo = $(config.renderTo);
    for (var i = 0; i < properties.length; i++) {
        if (config[properties[i]] != undefined)this[properties[i]] = config[properties[i]];
    }



    this.render();
};

$.extend(DG.ChessFen20.prototype, {

    lightSquare: undefined,
    darkSquare: undefined,

    font: "alpha",

    labels: true,
    labelFontSize: 1 / 20,

    imageFolder: undefined,

    renderTo: undefined,
    el: undefined,

    rankLabelContainer: undefined,
    fileLabelContainer: undefined,

    rankLabelElements: undefined,
    fileLabelElements: undefined,

    boardEl: undefined,

    fen: undefined,

    piecesContainer: undefined,

    autoFlip: true,

    colorToMove: "b",


    render: function () {
        this.el = $('<div class="chess-container"></div>');
        this.el.css("position", "relative");
        this.renderTo.append(this.el);

        if (this.labels) {
            this.renderRankLabels();
            this.renderFileLabels();
        }

        this.renderBoard();

        if (this.labels) {
            this.positionLabels();
        }

        this.renderTo.css({
            height: this.boardEl.height() + (this.labels ? this.fileLabelContainer.height() : 0)
        });

        if (this.fen != undefined) {
            this.showFen(this.fen);
        }
    },

    renderRankLabels: function () {
        var el = this.rankLabelContainer = $('<div class="fen-label-container fen-label-rank-container"></div>');
        el.css({
            position: "absolute",
            left: 0,
            top: 0
        });
        this.el.append(el);
        this.rankLabelElements = [];
        var fontSize = this.el.width() * this.labelFontSize;
        for (var i = 1; i <= 8; i++) {
            var rankEl = $("<div>" + (9 - i) + "</div>");
            rankEl.css({
                "font-size": fontSize,
                position: "absolute",
                "text-align": "center",
                "padding-right": fontSize / 5
            });
            el.append(rankEl);
            this.rankLabelElements.push(rankEl);
        }
        el.css("width", this.rankLabelElements[0].outerWidth());
    },

    renderFileLabels: function () {
        var el = this.fileLabelContainer = $('<div class="fen-label-container fen-label-file-container"></div>');
        el.css({
            position: "absolute",
            bottom: 0,
            left: 0,
            width: this.el.width() - this.rankLabelContainer.width()
        });
        this.el.append(el);

        this.fileLabelElements = [];
        var letters = "ABCDEFGH";
        for (var i = 0; i < 8; i++) {
            var rankEl = $("<div>" + letters.substr(i, 1) + "</div>");
            rankEl.css({
                "font-size": this.el.width() * this.labelFontSize,
                "float": "left",
                "width": (100 / 8) + "%",
                "text-align": "center",
                "padding-top": 2,
                "padding-bottom": 2
            });
            el.append(rankEl);
            this.fileLabelElements.push(rankEl);
        }
        el.css("height", this.rankLabelElements[0].height());

    },

    positionLabels: function () {
        for (var i = 0, len = this.rankLabelElements.length; i < len; i++) {
            this.rankLabelElements[i].css({
                height: this.squareSize,
                top: (this.squareSize * i) + (this.squareSize / 4)
            });
        }

        this.fileLabelContainer.css({
            top: this.squareSize * 8,
            left: this.boardEl.css("left")
        });

        for (i = 0, len = this.fileLabelElements.length; i < len; i++) {
            this.fileLabelElements[i].css({
                width: this.squareSize
            });
        }

    },

    updateLabels: function () {
        var letters = this.colorToMove == "w" ? "ABCDEFGH" : "HGFEDCBA";
        var digits = this.colorToMove == "w" ? "87654321" : "12345678";
        for (var i = 0, len = this.rankLabelElements.length; i < len; i++) {
            this.rankLabelElements[i].html(digits.substr(i, 1));
            this.fileLabelElements[i].html(letters.substr(i, 1));
        }
    },

    renderBoard: function () {
        var offset = this.labels ? Math.max(this.rankLabelContainer.width(), this.fileLabelContainer.height()) : 0;

        var availSize = this.el.width() - offset;
        availSize = Math.max(17 * 8, availSize);
        this.boardEl = $('<div class="chess-board"></div>');
        this.boardEl.css({
            "position": "relative",
            left: offset,
            width: availSize,
            height: availSize
        });
        this.el.append(this.boardEl);

        this.renderSquares();
    },

    renderSquares: function () {
        this.squareSize = Math.max(17, this.boardEl.height() / 8);

        for (var i = 0; i < 64; i++) {
            var square = $('<div class="fen-square"></div>');
            var file = i % 8;
            var rank = Math.floor(i / 8);
            var bgImage = (i + rank) % 2 == 1 ? this.darkSquare : this.lightSquare;
            square.css({
                position: "absolute",
                left: (file * this.squareSize),
                top: (rank * this.squareSize),
                width: this.squareSize, height: this.squareSize,
                "background-image": "url(" + this.imageFolder + bgImage + ")"
            });
            this.boardEl.append(square);
        }

        this.setPieceSize();
    },

    setPieceSize: function () {
        var size = this.squareSize - this.squareSize % 15;
        if (size < 30) {
            size = size < 22 ? 21 : 30
        }
        this.pieceSize = size;
    },


    showFen: function (fen) {
        this.fen = fen;
        var tokens = this.fen.split(/\s/g);
        if (tokens.length > 0) {
            this.colorToMove = tokens[1].toLowerCase();
        }
        this.renderPosition();
    },

    renderPosition: function () {

        if (this.labels)this.updateLabels();

        if (!this.piecesContainer) {
            var el = this.piecesContainer = $('<div class="fen-pieces-container"></div>');
            el.css({
                position: "absolute",
                top: 0,
                left: this.boardEl.css("left"),
                width: this.boardEl.width(),
                height: this.boardEl.height()
            });
            this.el.append(el);
        }

        this.piecesContainer.empty();
        this.createPieces();
    },

    createPieces: function () {
        var pos = this.fen.split(/\s/g)[0];

        var indexOnBoard = 0;
        for (var i = 0; i < pos.length; i++) {
            var item = pos.substr(i, 1);

            if ((/[a-z]/i).test(item)) {

                var xy = this.getXY(indexOnBoard);

                var el = $('<div class="fen-chess-piece"></div>');

                var color = /[a-z]/.test(item) ? "b" : "w";

                el.css({
                    position: "absolute",
                    left: xy.x,
                    width: this.squareSize, height: this.squareSize,
                    top: xy.y,
                    "background-repeat": "no-repeat",
                    "background-position": "center center",
                    "background-image": "url(" + this.imageFolder + this.font + this.pieceSize + color + item.toLowerCase() + ".png)"

                });
                this.piecesContainer.append(el);
                indexOnBoard++;
            } else if ((/[0-9]/).test(item)) {
                indexOnBoard += item / 1;
            }
        }
    },

    getXY: function (pos) {
        var file = pos % 8;
        var rank = Math.floor(pos / 8);
        if (this.colorToMove == "b") {
            file = 7 - file;
            rank = 7 - rank;
        }
        return {
            x: this.squareSize * file,
            y: this.squareSize * rank
        };
    }
});


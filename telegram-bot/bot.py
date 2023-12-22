import os
import telebot
import re
import json
import datetime

BOT_TOKEN = os.environ.get('BOT_TOKEN')
bot = telebot.TeleBot(BOT_TOKEN)
regex = '!тема [А-Яа-я0-9 !?.,:;]+'
topicsPath = '/Users/kirill_kravchenko/Developer/AI_stream/Assets/Scripts/Resources/topics.json'
topicPrefix = '!тема'

@bot.message_handler()
def command_help(message):
    list = re.split(regex, message.text)
    if (len(list) == 2):
        if (list[0] == '' and list[1] == ''):
            topic = str(message.text).replace(topicPrefix, '').strip()
            with open(topicsPath) as topics:
                data = json.load(topics)
            data.append(topic)
            with open(topicsPath, 'w') as topics:
                json.dump(data, topics, ensure_ascii=False)
            bot.reply_to(message, 'Тема принята! 👍🏼')
            print(str(datetime.datetime.now()) + ': тема \'' + topic + '\' принята')
    else:
        bot.reply_to(message, 'Формат темы некорректный. 👎🏻')
        print(str(datetime.datetime.now()) + ': формат \'' + message.text + '\' некорректный')

print("bot is running...")
bot.infinity_polling()
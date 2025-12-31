pipeline {
    agent any
    
    stages {
        stage('获取代码') {
            steps {
                checkout scm
            }
        }

        stage('清理旧容器和镜像') {
            steps {
                script {
                    echo '删除旧容器...'
                    sh 'docker rm -f orchardcore-app nextjs-frontend postgres-db || true'

                    echo '删除不可复用的镜像...'
                    // 删除所有打了 ci-build=true 标签的镜像
                    sh '''
                    for img in $(docker images --filter "label=ci-build=true" --format "{{.ID}}"); do
                        docker rmi -f $img
                    done
                    '''
                }
            }
        }
        
        stage('构建和部署') {
            steps {
                   dir('src') {  // 切换到 src 目录
                        script {
                            echo '开始构建和部署Docker服务...'
                            sh 'docker compose up --build -d'  // 不用 -f，默认找当前目录的 docker-compose.yml
                            echo 'Docker服务构建和部署完成！'
                        }
                    }
            }
        }
        
        stage('验证部署') {
            steps {
                dir('src') {
                    script {
                        echo '验证Docker服务状态...'
                        sh 'docker compose ps'
                    }
                }
            }
        }
    }
    
    post {
        always {
            echo '构建完成'
        }
        success {
            echo '部署成功！'
        }
        failure {
            echo '部署失败！'
        }
    }
}